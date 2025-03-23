using System;
using System.IO;
using System.Numerics;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ZoomiesPlugin.UI;
using ZoomiesPlugin.Helpers;
using ZoomiesPlugin.Renderers;

namespace ZoomiesPlugin.Core
{
    public sealed class Plugin : IDalamudPlugin
    {
        // The plugin name.
        public string Name => "Zoomies";

        // Plugin services injected via attributes.
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;

        // Command name constant - only keeping one command
        private const string ZoomiesCommandName = "/zoomies";

        // Public configuration object.
        public Configuration Configuration { get; init; }

        // The WindowSystem manages all plugin windows.
        public readonly WindowSystem WindowSystem = new("ZoomiesPlugin");

        // All windows
        private SpeedometerWindow SpeedometerWindow { get; init; }
        private NyanCatWindow NyanCatWindow { get; init; }
        private DebugWindow DebugWindow { get; init; }
        private ConfigWindow ConfigWindow { get; init; }

        // Texture for the Nyan Cat
        private static IntPtr nyanCatTextureHandle = IntPtr.Zero;
        private static Vector2 nyanCatTextureSize = new Vector2(0, 0);

        // Texture loading fields
        private string textureToLoad = string.Empty;
        private bool isCustomTexture = false;

        // Plugin constructor.
        public Plugin()
        {
            // Load the configuration
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // Check if nyan.png exists and load it
            CheckForNyanCatImage();

            // Check for user-provided custom images
            LoadUserImages();

            // Create windows
            SpeedometerWindow = new SpeedometerWindow();
            NyanCatWindow = new NyanCatWindow();

            // Create debug window with reference to calculator
            DebugWindow = new DebugWindow(SpeedometerWindow.GetCalculator(), this);

            // Create config window
            ConfigWindow = new ConfigWindow(this);

            // Add windows to the WindowSystem
            WindowSystem.AddWindow(SpeedometerWindow);
            WindowSystem.AddWindow(NyanCatWindow);
            WindowSystem.AddWindow(DebugWindow);
            WindowSystem.AddWindow(ConfigWindow);

            // Register command handler - just one command now
            CommandManager.AddHandler(ZoomiesCommandName, new CommandInfo(OnZoomiesCommand)
            {
                HelpMessage = "Toggle the Zoomies speedometer or open the UI if using /zoomies config"
            });

            // Subscribe to UI drawing event
            PluginInterface.UiBuilder.Draw += DrawUI;

            // Use config button to open config UI
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            // Use main button to toggle speedometer
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

            // Subscribe to login/logout events for auto-show/hide
            ClientState.Login += OnLogin;
            ClientState.Logout += OnLogout;

            // Set initial state based on config and login status
            if (ClientState.IsLoggedIn)
            {
                SetInitialState();
            }
            else
            {
                // Hide all windows if not logged in
                HideAllSpeedometers();
                ConfigWindow.IsOpen = false;
                DebugWindow.IsOpen = false;
            }

            // Log an informational message
            Log.Information($"===Zoomies Plugin loaded===");
        }

        private void SetInitialState()
        {
            // Hide all windows initially
            SpeedometerWindow.IsOpen = false;
            NyanCatWindow.IsOpen = false;
            DebugWindow.IsOpen = false;

            // Show the appropriate speedometer if configured
            if (Configuration.ShowSpeedometerOnStartup)
            {
                switch (Configuration.SelectedSpeedometerType)
                {
                    case 0:
                        SpeedometerWindow.IsOpen = true;
                        break;
                    case 1:
                        NyanCatWindow.IsOpen = true;
                        break;
                }
            }
        }

        // Login event handler
        private void OnLogin()
        {
            Log.Information("Login detected, showing speedometer if configured.");

            // Restore the appropriate speedometer if configured
            if (Configuration.ShowSpeedometerOnStartup)
            {
                switch (Configuration.SelectedSpeedometerType)
                {
                    case 0:
                        SpeedometerWindow.IsOpen = true;
                        break;
                    case 1:
                        NyanCatWindow.IsOpen = true;
                        break;
                }
            }
        }

        // Logout event handler
        private void OnLogout(int type, int code)
        {
            Log.Information($"Logout detected. Type: {type}, Code: {code}");

            // Hide all speedometers
            SpeedometerWindow.IsOpen = false;
            NyanCatWindow.IsOpen = false;

            // Optionally hide other windows too
            ConfigWindow.IsOpen = false;
            DebugWindow.IsOpen = false;
        }

        // Check if nyan.png exists in the plugin directory and load it
        private void CheckForNyanCatImage()
        {
            try
            {
                // Build the path to the image file
                string pluginPath = PluginInterface.AssemblyLocation.DirectoryName ?? string.Empty;
                string imagePath = Path.Combine(pluginPath, "nyan.png");

                Log.Information($"Looking for nyan.png at: {imagePath}");

                if (File.Exists(imagePath))
                {
                    // Just store the path for now - we'll load the texture on the main thread
                    LoadTextureOnNextDraw(imagePath, false);
                }
                else
                {
                    Log.Error($"Nyan cat image not found at: {imagePath}");
                    Log.Information("Using drawn Nyan Cat as fallback");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error checking for nyan.png: {ex.Message}");
            }
        }

        // Check for and load user-provided custom images
        private void LoadUserImages()
        {
            try
            {
                // Get plugin config directory
                string configDir = PluginInterface.GetPluginConfigDirectory();
                string imagesDir = Path.Combine(configDir, "images");

                // Create the directory if it doesn't exist
                if (!Directory.Exists(imagesDir))
                {
                    Directory.CreateDirectory(imagesDir);
                    Log.Information($"Created images directory at: {imagesDir}");
                    return; // No images to load yet
                }

                // Check if user has provided a custom nyan.png
                string customNyanPath = Path.Combine(imagesDir, "nyan.png");
                if (File.Exists(customNyanPath))
                {
                    Log.Information($"Found custom nyan.png at: {customNyanPath}");

                    // Schedule loading on the main thread
                    LoadTextureOnNextDraw(customNyanPath, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading user images: {ex.Message}");
            }
        }

        // Schedule a texture to be loaded on the main thread
        private void LoadTextureOnNextDraw(string path, bool isCustom)
        {
            textureToLoad = path;
            isCustomTexture = isCustom;
        }

        // Static methods to access the texture info
        public static IntPtr GetNyanCatTextureHandle()
        {
            return nyanCatTextureHandle;
        }

        public static Vector2 GetNyanCatTextureSize()
        {
            return nyanCatTextureSize;
        }

        // Dispose method
        public void Dispose()
        {
            // Remove all windows
            WindowSystem.RemoveAllWindows();

            // Dispose of windows
            SpeedometerWindow.Dispose();
            NyanCatWindow.Dispose();
            DebugWindow.Dispose();
            ConfigWindow.Dispose();

            // Unsubscribe from events
            ClientState.Login -= OnLogin;
            ClientState.Logout -= OnLogout;
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

            // Remove command handler
            CommandManager.RemoveHandler(ZoomiesCommandName);
        }

        // Command handler - now supports args for config
        private void OnZoomiesCommand(string command, string args)
        {
            if (args.Trim().ToLower() == "config")
            {
                ConfigWindow.IsOpen = true;
            }
            else
            {
                ToggleSpeedometer();
            }
        }

        // Draw UI
        private void DrawUI()
        {
            // If we have a texture to load, do it now (we're on the main thread)
            if (!string.IsNullOrEmpty(textureToLoad))
            {
                try
                {
                    var textureResult = TextureProvider.GetFromFile(textureToLoad);
                    if (textureResult != null)
                    {
                        var texture = textureResult.GetWrapOrDefault();
                        if (texture != null)
                        {
                            nyanCatTextureHandle = texture.ImGuiHandle;
                            nyanCatTextureSize = new Vector2(texture.Width, texture.Height);
                            Log.Information($"Successfully loaded {(isCustomTexture ? "custom " : "")}nyan.png texture");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception during texture loading: {ex.Message}");
                    if (!isCustomTexture)
                        Log.Information("Using drawn Nyan Cat as fallback");
                }

                // Clear the texture path to prevent loading it again
                textureToLoad = string.Empty;
            }

            // Draw windows
            WindowSystem.Draw();
        }

        // UI toggling methods
        public void ToggleConfigUI()
        {
            ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
        }

        public void ToggleMainUI()
        {
            ToggleSpeedometer();
        }

        public void ToggleDebugUI()
        {
            DebugWindow.IsOpen = !DebugWindow.IsOpen;
        }

        // Speedometer management methods
        public void ToggleSpeedometer()
        {
            bool isAnyVisible = IsAnySpeedometerVisible();

            if (isAnyVisible)
            {
                HideAllSpeedometers();
            }
            else
            {
                switch (Configuration.SelectedSpeedometerType)
                {
                    case 0:
                        SwitchToClassicSpeedometer();
                        break;
                    case 1:
                        SwitchToNyanSpeedometer();
                        break;
                }
            }
        }

        public void SwitchToClassicSpeedometer()
        {
            NyanCatWindow.IsOpen = false;
            SpeedometerWindow.IsOpen = true;

            Configuration.SelectedSpeedometerType = 0;
            Configuration.ShowSpeedometerOnStartup = true;
            Configuration.Save();
        }

        public void SwitchToNyanSpeedometer()
        {
            SpeedometerWindow.IsOpen = false;
            NyanCatWindow.IsOpen = true;

            Configuration.SelectedSpeedometerType = 1;
            Configuration.ShowSpeedometerOnStartup = true;
            Configuration.Save();
        }

        public void HideAllSpeedometers()
        {
            SpeedometerWindow.IsOpen = false;
            NyanCatWindow.IsOpen = false;

            Configuration.ShowSpeedometerOnStartup = false;
            Configuration.Save();
        }

        public bool IsAnySpeedometerVisible()
        {
            return SpeedometerWindow.IsOpen || NyanCatWindow.IsOpen;
        }

        // Settings update methods
        public void UpdateMaxSpeed(float maxSpeed)
        {
            SpeedometerWindow.UpdateMaxSpeed(maxSpeed);
            NyanCatWindow.UpdateMaxSpeed(maxSpeed);
        }

        public void UpdateRedlineStart(float redlineStart)
        {
            SpeedometerWindow.UpdateRedlineStart(redlineStart);
        }

        public void UpdateDamping(float damping)
        {
            SpeedometerWindow.UpdateDamping(damping);
            NyanCatWindow.UpdateDamping(damping);
        }
    }
}

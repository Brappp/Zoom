using System;
using System.IO;
using System.Numerics; // Added for Vector2
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ZoomiesPlugin.Windows;

namespace ZoomiesPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        // The plugin name.
        public string Name => "Yalms";

        // Plugin services injected via attributes.
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;

        // Command name constants.
        private const string ZoomiesCommandName = "/zoomies";
        private const string DebugCommandName = "/zoomiesdebug";
        private const string ConfigCommandName = "/speedconfig";

        // Public configuration object.
        public Configuration Configuration { get; init; }

        // The WindowSystem manages all plugin windows.
        public readonly WindowSystem WindowSystem = new("YalmsPlugin");

        // Windows
        private YalmsWindow YalmsWindow { get; init; }
        private DebugWindow DebugWindow { get; init; }
        private NyanCatWindow NyanCatWindow { get; init; }
        private SpeedometerConfigWindow ConfigWindow { get; init; }

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

            // Instantiate the windows
            YalmsWindow = new YalmsWindow();
            NyanCatWindow = new NyanCatWindow();

            // Create debug window with reference to this plugin instance
            DebugWindow = new DebugWindow(YalmsWindow.GetCalculator(), this);

            // Create config window
            ConfigWindow = new SpeedometerConfigWindow(this);

            // Add windows to the WindowSystem
            WindowSystem.AddWindow(YalmsWindow);
            WindowSystem.AddWindow(DebugWindow);
            WindowSystem.AddWindow(NyanCatWindow);
            WindowSystem.AddWindow(ConfigWindow);

            // Register command handlers
            CommandManager.AddHandler(ZoomiesCommandName, new CommandInfo(OnZoomiesCommand)
            {
                HelpMessage = "Toggles the Yalms speedometer window"
            });

            CommandManager.AddHandler(DebugCommandName, new CommandInfo(OnDebugCommand)
            {
                HelpMessage = "Opens the Yalms debug calculation window"
            });

            CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Opens the speedometer configuration window"
            });

            // Subscribe to UI drawing event
            PluginInterface.UiBuilder.Draw += DrawUI;

            // Use config button to open config window
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            // Use main button to toggle speedometer
            PluginInterface.UiBuilder.OpenMainUi += ToggleSpeedometerUI;

            // Log an informational message
            Log.Information($"===Yalms Plugin loaded===");

            // Set initial speedometer based on saved configuration
            SetInitialSpeedometerState();
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
                    var fileInfo = new FileInfo(imagePath);
                    Log.Information($"Found nyan.png at: {imagePath}");
                    Log.Information($"File size: {fileInfo.Length} bytes");
                    Log.Information($"Creation time: {fileInfo.CreationTime}");
                    Log.Information($"Last write time: {fileInfo.LastWriteTime}");
                    Log.Information($"Attributes: {fileInfo.Attributes}");

                    // Check if file can be read
                    try
                    {
                        using (var fileStream = File.OpenRead(imagePath))
                        {
                            Log.Information($"File can be opened for reading");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"File cannot be opened for reading: {ex.Message}");
                    }

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
                Log.Error($"Stack trace: {ex.StackTrace}");
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

        // Set initial speedometer state based on configuration
        private void SetInitialSpeedometerState()
        {
            // Initially hide all speedometers
            YalmsWindow.IsOpen = false;
            NyanCatWindow.IsOpen = false;
            DebugWindow.IsOpen = false;

            // Show the selected speedometer if configuration specifies it should be shown
            if (Configuration.ShowSpeedometerOnStartup)
            {
                switch (Configuration.SelectedSpeedometerType)
                {
                    case 0:
                        YalmsWindow.IsOpen = true;
                        break;
                    case 1:
                        NyanCatWindow.IsOpen = true;
                        break;
                    default:
                        YalmsWindow.IsOpen = true;
                        break;
                }
            }

            // Update all speedometers with current configuration values
            UpdateAllSpeedometerSettings();
        }

        // Dispose method
        public void Dispose()
        {
            // Remove all windows
            WindowSystem.RemoveAllWindows();

            // Dispose of windows
            YalmsWindow.Dispose();
            DebugWindow.Dispose();
            NyanCatWindow.Dispose();
            ConfigWindow.Dispose();

            // Remove command handlers
            CommandManager.RemoveHandler(ZoomiesCommandName);
            CommandManager.RemoveHandler(DebugCommandName);
            CommandManager.RemoveHandler(ConfigCommandName);
        }

        // Command handlers
        private void OnZoomiesCommand(string command, string args)
        {
            ToggleSpeedometerUI();
        }

        private void OnDebugCommand(string command, string args)
        {
            ToggleDebugUI();
        }

        private void OnConfigCommand(string command, string args)
        {
            ToggleConfigUI();
        }

        // Draw UI
        private void DrawUI()
        {
            // If we have a texture to load, do it now (we're on the main thread)
            if (!string.IsNullOrEmpty(textureToLoad))
            {
                try
                {
                    Log.Information($"Starting texture load process for: {textureToLoad}");
                    Log.Information($"Is custom texture: {isCustomTexture}");
                    Log.Information($"File exists check: {File.Exists(textureToLoad)}");
                    Log.Information($"File size: {new FileInfo(textureToLoad).Length} bytes");

                    Log.Information("Calling TextureProvider.GetFromFile...");
                    var textureResult = TextureProvider.GetFromFile(textureToLoad);
                    Log.Information($"GetFromFile result is null: {textureResult == null}");

                    if (textureResult != null)
                    {
                        Log.Information("Calling GetWrapOrDefault...");
                        var texture = textureResult.GetWrapOrDefault();
                        Log.Information($"GetWrapOrDefault result is null: {texture == null}");

                        if (texture != null)
                        {
                            Log.Information($"Texture Width: {texture.Width}, Height: {texture.Height}");
                            Log.Information($"ImGuiHandle: {texture.ImGuiHandle}");

                            nyanCatTextureHandle = texture.ImGuiHandle;
                            nyanCatTextureSize = new Vector2(texture.Width, texture.Height);
                            Log.Information($"Successfully loaded {(isCustomTexture ? "custom " : "")}nyan.png texture ({nyanCatTextureSize.X}x{nyanCatTextureSize.Y})");

                            if (isCustomTexture && nyanCatTextureHandle != IntPtr.Zero)
                            {
                                Log.Information("Replacing default nyan.png with custom version");
                            }
                        }
                        else
                        {
                            Log.Error($"GetWrapOrDefault returned null for texture");
                        }
                    }
                    else
                    {
                        Log.Error($"GetFromFile returned null for {textureToLoad}");
                    }

                    if (nyanCatTextureHandle == IntPtr.Zero)
                    {
                        Log.Error($"Failed to load {(isCustomTexture ? "custom " : "")}nyan.png texture");
                        if (!isCustomTexture)
                            Log.Information("Using drawn Nyan Cat as fallback");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception during texture loading: {ex.Message}");
                    Log.Error($"Stack trace: {ex.StackTrace}");
                    if (!isCustomTexture)
                        Log.Information("Using drawn Nyan Cat as fallback");
                }

                // Clear the texture path to prevent loading it again
                textureToLoad = string.Empty;
            }

            // Draw windows
            WindowSystem.Draw();
        }

        // Toggle windows
        public void ToggleConfigUI() => ConfigWindow.Toggle();

        public void ToggleDebugUI() => DebugWindow.Toggle();

        // Toggle the speedometer (shows/hides the currently selected type)
        public void ToggleSpeedometerUI()
        {
            if (IsAnySpeedometerVisible())
            {
                HideAllSpeedometers();
            }
            else
            {
                // Show the speedometer based on saved selection
                switch (Configuration.SelectedSpeedometerType)
                {
                    case 0:
                        SwitchToClassicSpeedometer();
                        break;
                    case 1:
                        SwitchToNyanSpeedometer();
                        break;
                    default:
                        SwitchToClassicSpeedometer();
                        break;
                }
            }
        }

        // Method to switch to the classic speedometer
        public void SwitchToClassicSpeedometer()
        {
            // Hide other speedometers
            NyanCatWindow.IsOpen = false;
            // Show the classic speedometer
            YalmsWindow.IsOpen = true;

            // Update the configuration
            Configuration.SelectedSpeedometerType = 0;
            Configuration.Save();
        }

        // Method to switch to the Nyan Cat speedometer
        public void SwitchToNyanSpeedometer()
        {
            // Hide other speedometers
            YalmsWindow.IsOpen = false;
            // Show the Nyan Cat speedometer
            NyanCatWindow.IsOpen = true;

            // Update the configuration
            Configuration.SelectedSpeedometerType = 1;
            Configuration.Save();
        }

        // Hide all speedometers
        public void HideAllSpeedometers()
        {
            YalmsWindow.IsOpen = false;
            NyanCatWindow.IsOpen = false;
        }

        // Check if any speedometer is currently visible
        public bool IsAnySpeedometerVisible()
        {
            return YalmsWindow.IsOpen || NyanCatWindow.IsOpen;
        }

        // Check if the Nyan speedometer is active
        public bool IsNyanSpeedometerActive()
        {
            return NyanCatWindow.IsOpen;
        }

        // Update speedometer settings
        public void UpdateMaxSpeed(float maxSpeed)
        {
            // Update classic speedometer
            if (YalmsWindow is YalmsWindow yalms)
            {
                if (yalms.GetRenderer() is YalmsRenderer renderer)
                {
                    renderer.SetMaxYalms(maxSpeed);
                }
            }

            // Update Nyan Cat speedometer
            if (NyanCatWindow is NyanCatWindow nyan)
            {
                if (nyan.GetRenderer() is NyanCatRenderer renderer)
                {
                    renderer.SetMaxYalms(maxSpeed);
                }
            }
        }

        public void UpdateRedlineStart(float redlineStart)
        {
            // Update classic speedometer
            if (YalmsWindow is YalmsWindow yalms)
            {
                if (yalms.GetRenderer() is YalmsRenderer renderer)
                {
                    renderer.SetRedlineStart(redlineStart);
                }
            }
        }

        public void UpdateDamping(float damping)
        {
            // Update classic calculator
            if (YalmsWindow is YalmsWindow yalms)
            {
                if (yalms.GetCalculator() is YalmsCalculator calculator)
                {
                    calculator.SetDamping(damping);
                }
            }

            // Update Nyan Cat calculator
            if (NyanCatWindow is NyanCatWindow nyan)
            {
                if (nyan.GetCalculator() is YalmsCalculator calculator)
                {
                    calculator.SetDamping(damping);
                }
            }
        }

        // Update all settings at once
        private void UpdateAllSpeedometerSettings()
        {
            UpdateMaxSpeed(Configuration.MaxYalms);
            UpdateRedlineStart(Configuration.RedlineStart);
            UpdateDamping(Configuration.NeedleDamping);
        }
    }
}

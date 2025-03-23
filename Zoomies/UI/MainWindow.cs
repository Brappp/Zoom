using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ZoomiesPlugin.Core;
using ZoomiesPlugin.Helpers;

namespace ZoomiesPlugin.UI
{
    public class MainWindow : Window, IDisposable
    {
        // Reference to plugin for settings
        private readonly Plugin plugin;

        // Reference to other windows
        private readonly SpeedometerWindow speedometerWindow;
        private readonly NyanCatWindow nyanCatWindow;
        private readonly DebugWindow debugWindow;
        private readonly ConfigWindow configWindow;

        // Constructor
        public MainWindow(Plugin pluginInstance,
                          SpeedometerWindow speedWindow,
                          NyanCatWindow nyanWindow,
                          DebugWindow debugWin,
                          ConfigWindow configWin) : base("Zoomies##MainWindow")
        {
            plugin = pluginInstance;
            speedometerWindow = speedWindow;
            nyanCatWindow = nyanWindow;
            debugWindow = debugWin;
            configWindow = configWin;

            // Set window size and flags
            Size = new Vector2(300, 80);
            SizeCondition = ImGuiCond.FirstUseEver;

            // Initialize based on configuration
            var config = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            if (config.ShowSpeedometerOnStartup)
            {
                ShowSpeedometer(config.SelectedSpeedometerType);
            }
        }

        public override void Draw()
        {
            ImGui.Text("Zoomies Speedometer Controls");
            ImGui.Separator();

            if (ImGui.Button("Toggle Speedometer"))
            {
                ToggleSpeedometer();
            }

            ImGui.SameLine();

            if (ImGui.Button("Configure"))
            {
                configWindow.IsOpen = true;
            }

            ImGui.SameLine();

            if (ImGui.Button("Debug"))
            {
                debugWindow.IsOpen = !debugWindow.IsOpen;
            }
        }

        // Show the speedometer of specified type
        public void ShowSpeedometer(int type)
        {
            // Hide all first
            speedometerWindow.IsOpen = false;
            nyanCatWindow.IsOpen = false;

            // Show selected one
            switch (type)
            {
                case 0:
                    speedometerWindow.IsOpen = true;
                    break;
                case 1:
                    nyanCatWindow.IsOpen = true;
                    break;
            }
        }

        // Public method to toggle speedometer
        public void ToggleSpeedometer()
        {
            var config = Plugin.PluginInterface.GetPluginConfig() as Configuration;
            if (config != null)
            {
                bool isAnyVisible = speedometerWindow.IsOpen || nyanCatWindow.IsOpen;

                if (isAnyVisible)
                {
                    // Hide all speedometers
                    speedometerWindow.IsOpen = false;
                    nyanCatWindow.IsOpen = false;
                }
                else
                {
                    // Show the appropriate speedometer
                    ShowSpeedometer(config.SelectedSpeedometerType);
                }

                // Update config
                config.ShowSpeedometerOnStartup = !isAnyVisible;
                config.Save();
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}

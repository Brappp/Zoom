using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ZoomiesPlugin.Core;

namespace ZoomiesPlugin.UI
{
    public class ConfigWindow : Window, IDisposable
    {
        private Configuration Configuration;
        private readonly Plugin Plugin;

        // Speedometer type selection
        private int selectedSpeedometerType = 0;
        private readonly string[] speedometerTypes = new string[]
        {
            "Classic Gauge",
            "Nyan Cat"
        };

        // Tooltip display toggle
        private bool showFormula = false;

        public ConfigWindow(Plugin plugin) : base("Zoomies Configuration##ConfigWindow")
        {
            Plugin = plugin;
            Configuration = plugin.Configuration;

            // Initialize selected speedometer based on current state
            selectedSpeedometerType = Configuration.SelectedSpeedometerType;

            // Set window size and flags
            Size = new Vector2(350, 200);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public void Dispose() { }

        public override void Draw()
        {
            // Title
            ImGui.Text("Zoomies Speedometer Configuration");
            ImGui.Separator();

            // Speedometer style selection
            ImGui.Text("Speedometer Style:");
            if (ImGui.Combo("##SpeedometerType", ref selectedSpeedometerType, speedometerTypes, speedometerTypes.Length))
            {
                // Switch speedometer type based on selection
                switch (selectedSpeedometerType)
                {
                    case 0: // Classic Gauge
                        Plugin.SwitchToClassicSpeedometer();
                        break;
                    case 1: // Nyan Cat
                        Plugin.SwitchToNyanSpeedometer();
                        break;
                }

                // Save the selection to configuration
                Configuration.SelectedSpeedometerType = selectedSpeedometerType;
                Configuration.Save();
            }

            ImGui.Spacing();

            // Redline configuration
            float redlineStart = Configuration.RedlineStart;
            if (ImGui.SliderFloat("Redline Start (yalms/s)", ref redlineStart, 5.0f, Configuration.MaxYalms, "%.1f"))
            {
                Configuration.RedlineStart = redlineStart;
                Configuration.Save();

                // Update speedometers with new redline
                Plugin.UpdateRedlineStart(redlineStart);
            }

            // Max speed configuration
            float maxYalms = Configuration.MaxYalms;
            if (ImGui.SliderFloat("Max Speed (yalms/s)", ref maxYalms, 10.0f, 100.0f, "%.1f"))
            {
                Configuration.MaxYalms = maxYalms;
                Configuration.Save();

                // Update speedometers with new max speed
                Plugin.UpdateMaxSpeed(maxYalms);
            }

            // Needle smoothing configuration
            float damping = Configuration.NeedleDamping;
            if (ImGui.SliderFloat("Needle Smoothing", ref damping, 0.01f, 1.0f, "%.2f"))
            {
                Configuration.NeedleDamping = damping;
                Configuration.Save();

                // Update speedometers with new smoothing value
                Plugin.UpdateDamping(damping);
            }

            ImGui.Spacing();
            ImGui.Separator();

            // Show speedometer toggle
            bool showSpeedometer = Plugin.IsAnySpeedometerVisible();
            if (ImGui.Checkbox("Show Speedometer", ref showSpeedometer))
            {
                if (showSpeedometer)
                {
                    // Show the currently selected speedometer
                    switch (selectedSpeedometerType)
                    {
                        case 0:
                            Plugin.SwitchToClassicSpeedometer();
                            break;
                        case 1:
                            Plugin.SwitchToNyanSpeedometer();
                            break;
                    }
                }
                else
                {
                    // Hide all speedometers
                    Plugin.HideAllSpeedometers();
                }

                // Save to config
                Configuration.ShowSpeedometerOnStartup = showSpeedometer;
                Configuration.Save();
            }

            ImGui.Spacing();

            // Debug button
            if (ImGui.Button("Open Debug Window"))
            {
                Plugin.ToggleDebugUI();
            }

            ImGui.SameLine();

            // Formula toggle
            if (ImGui.Button(showFormula ? "Hide Formula" : "Show Formula"))
            {
                showFormula = !showFormula;
            }

            // Show formula explanation if toggled
            if (showFormula)
            {
                ImGui.BeginChild("FormulaExplanation", new Vector2(ImGui.GetContentRegionAvail().X, 100), true);

                ImGui.TextWrapped("The speedometer uses this formula to calculate your speed:");
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), "Speed (yalms/second) = Horizontal Distance / Time");
                ImGui.Spacing();
                ImGui.TextWrapped("Only horizontal movement (X and Z axes) is measured, with Y-axis (up/down) movement ignored. The needle is smoothed using damping for more natural movement.");

                ImGui.EndChild();
            }
        }
    }
}

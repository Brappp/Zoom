using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ZoomiesPlugin.Windows
{
    public class DebugWindow : Window, IDisposable
    {
        // Stores position and time data for debugging
        private Vector3 currentPosition;
        private Vector3 previousPosition;
        private DateTime currentTime;
        private DateTime previousTime;
        private float distanceTraveled;
        private float deltaTime;
        private float currentSpeed;
        private float displaySpeed;

        // Mode settings
        private bool showSimpleMode = true;
        private bool showAdvancedInfo = false;
        private bool showHistoryTable = false;

        // Speedometer type selection
        private int selectedSpeedometerType = 0;
        private readonly string[] speedometerTypes = new string[]
        {
            "Classic Gauge",
            "Nyan Cat"
            // Add more speedometer types here as they're developed
        };

        // For displaying past calculations
        private readonly List<(DateTime time, float distance, float deltaTime, float speed)> calculationHistory;
        private const int MaxHistoryEntries = 20;

        // Reference to the main calculator for getting values
        private readonly YalmsCalculator yalmsCalculator;

        // Reference to the plugin to toggle speedometer types
        private readonly Plugin plugin;

        public DebugWindow(YalmsCalculator calculator, Plugin pluginInstance) : base("Yalms Speed Calculation##DebugWindow",
            ImGuiWindowFlags.AlwaysAutoResize)
        {
            yalmsCalculator = calculator;
            plugin = pluginInstance;
            calculationHistory = new List<(DateTime, float, float, float)>();

            // Initialize with zeros
            currentPosition = Vector3.Zero;
            previousPosition = Vector3.Zero;
            currentTime = DateTime.Now;
            previousTime = DateTime.Now;
            distanceTraveled = 0;
            deltaTime = 0;
            currentSpeed = 0;
            displaySpeed = 0;
        }

        public override void Draw()
        {
            var localPlayer = Plugin.ClientState.LocalPlayer;
            if (localPlayer != null)
            {
                // Update current values for display
                currentPosition = localPlayer.Position;

                // Get access to internal YalmsCalculator values through its public interface
                displaySpeed = yalmsCalculator.GetDisplayYalms();

                // Calculate values for display
                if (previousPosition != Vector3.Zero)
                {
                    Vector2 horizontalDelta = new Vector2(
                        currentPosition.X - previousPosition.X,
                        currentPosition.Z - previousPosition.Z
                    );
                    distanceTraveled = horizontalDelta.Length();
                    deltaTime = (float)(currentTime - previousTime).TotalSeconds;

                    // Only calculate speed if enough time passed
                    if (deltaTime > 0.01f)
                    {
                        currentSpeed = distanceTraveled / deltaTime;

                        // Add to history if values changed
                        if (distanceTraveled > 0.001f)
                        {
                            calculationHistory.Insert(0, (currentTime, distanceTraveled, deltaTime, currentSpeed));

                            // Keep history at manageable size
                            if (calculationHistory.Count > MaxHistoryEntries)
                                calculationHistory.RemoveAt(calculationHistory.Count - 1);
                        }

                        // Update previous values
                        previousPosition = currentPosition;
                        previousTime = currentTime;
                    }
                }
                else
                {
                    // Initialize previous values if first run
                    previousPosition = currentPosition;
                    previousTime = currentTime;
                }

                currentTime = DateTime.Now;

                // Toggle buttons for different views
                if (ImGui.Button(showSimpleMode ? "Show Advanced View" : "Show Simple View"))
                {
                    showSimpleMode = !showSimpleMode;
                }

                ImGui.SameLine();
                if (ImGui.Button(showHistoryTable ? "Hide History" : "Show History"))
                {
                    showHistoryTable = !showHistoryTable;
                }

                if (!showSimpleMode)
                {
                    ImGui.SameLine();
                    if (ImGui.Button(showAdvancedInfo ? "Hide Technical Details" : "Show Technical Details"))
                    {
                        showAdvancedInfo = !showAdvancedInfo;
                    }
                }

                ImGui.Separator();

                // Simple Mode - Very clear explanation
                if (showSimpleMode)
                {
                    DrawSimpleMode();
                }
                else
                {
                    DrawAdvancedMode();
                }

                // History table (optional)
                if (showHistoryTable)
                {
                    DrawHistoryTable();
                }
            }
            else
            {
                ImGui.Text("Player not available");
            }
        }

        private void DrawSimpleMode()
        {
            // Big speed display - no font change (using default font to avoid crashes)
            ImGui.Text(string.Format("{0:F1} yalms/s", displaySpeed));

            ImGui.Spacing();

            // Simple explanation
            ImGui.Text("How this is calculated:");
            ImGui.Spacing();

            ImGui.Indent(10);

            // Show visual of the calculation
            ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.5f, 1.0f), "Step 1: Measure how far your character moved");
            ImGui.Text($"→ You moved {distanceTraveled:F2} yalms");
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.5f, 1.0f), "Step 2: Measure how much time passed");
            ImGui.Text($"→ Time passed: {deltaTime:F2} seconds");
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.5f, 1.0f), "Step 3: Divide distance by time");
            ImGui.Text($"→ {distanceTraveled:F2} ÷ {deltaTime:F2} = {currentSpeed:F2} yalms/second");
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.5f, 1.0f), "Step 4: Smooth the value for better display");
            ImGui.Text($"→ Smoothed speed: {displaySpeed:F2} yalms/second");

            ImGui.Unindent(10);

            // Extra notes
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.5f, 1.0f), "Note: Only horizontal movement is measured (no up/down)");
        }

        private void DrawAdvancedMode()
        {
            // Current speed with formula
            ImGui.Text("Current Speed: ");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.5f, 1.0f), $"{displaySpeed:F2} yalms/second");

            ImGui.Text("Formula: Speed = Distance ÷ Time");

            // Visual calculation
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.5f, 1.0f), "Calculation:");
            ImGui.Text($"{currentSpeed:F2} = {distanceTraveled:F2} ÷ {deltaTime:F2}");

            // Raw values and smoothing
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.5f, 1.0f), "Speed Values:");
            ImGui.Text($"Raw Speed: {currentSpeed:F2} yalms/s");
            ImGui.Text($"Smoothed Speed: {displaySpeed:F2} yalms/s");

            // Show technical details if enabled
            if (showAdvancedInfo)
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1.0f, 1.0f), "Technical Details:");
                ImGui.Text($"Current Position: X:{currentPosition.X:F1} Y:{currentPosition.Y:F1} Z:{currentPosition.Z:F1}");
                ImGui.Text($"Previous Position: X:{previousPosition.X:F1} Y:{previousPosition.Y:F1} Z:{previousPosition.Z:F1}");

                // Show how horizontal distance is calculated
                Vector2 horizontalDelta = new Vector2(
                    currentPosition.X - previousPosition.X,
                    currentPosition.Z - previousPosition.Z
                );
                ImGui.Text($"X Distance: {horizontalDelta.X:F3}");
                ImGui.Text($"Z Distance: {horizontalDelta.Y:F3}");
                ImGui.Text($"Horizontal Distance: √({horizontalDelta.X:F2}² + {horizontalDelta.Y:F2}²) = {horizontalDelta.Length():F3}");
            }
        }

        private void DrawHistoryTable()
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), "Recent Measurements:");

            if (ImGui.BeginTable("history_table", 4, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Time");
                ImGui.TableSetupColumn("Distance (yalms)");
                ImGui.TableSetupColumn("Time (sec)");
                ImGui.TableSetupColumn("Speed (yalms/s)");
                ImGui.TableHeadersRow();

                foreach (var entry in calculationHistory)
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(entry.time.ToString("HH:mm:ss.ff"));

                    ImGui.TableNextColumn();
                    ImGui.Text($"{entry.distance:F3}");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{entry.deltaTime:F3}");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{entry.speed:F3}");
                }

                ImGui.EndTable();
            }

            // Add button to clear history
            if (ImGui.Button("Clear History"))
            {
                calculationHistory.Clear();
            }
        }

        public void Dispose()
        {
            // No unmanaged resources to clean up
        }
    }
}

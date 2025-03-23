using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ZoomiesPlugin.Core;
using ZoomiesPlugin.Helpers;
using ZoomiesPlugin.Renderers;

namespace ZoomiesPlugin.UI
{
    public class SpeedometerWindow : Window, IDisposable
    {
        // Helper classes for calculations and rendering
        private readonly YalmsCalculator yalmsCalculator;
        private readonly ClassicRenderer classicRenderer;

        // Constructor
        public SpeedometerWindow() : base("Zoomies##SpeedometerWindow",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize)
        {
            // Set a default size for the window
            Size = new Vector2(350, 350);
            SizeCondition = ImGuiCond.FirstUseEver;

            // Disable ESC key closing the window
            RespectCloseHotkey = false;

            // Get configuration
            var config = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // Create helper classes
            yalmsCalculator = new YalmsCalculator();
            classicRenderer = new ClassicRenderer();

            // Configure helpers from config
            yalmsCalculator.SetDamping(config.NeedleDamping);
            classicRenderer.SetMaxYalms(config.MaxYalms);
            classicRenderer.SetRedlineStart(config.RedlineStart);
        }

        // The Draw() method is called each frame to render the window
        public override void Draw()
        {
            // Update speed calculation
            var localPlayer = Plugin.ClientState.LocalPlayer;
            if (localPlayer != null)
            {
                // Update speed based on player position
                yalmsCalculator.Update(localPlayer.Position);
            }
            else
            {
                // Reset if player is not available
                yalmsCalculator.Reset();
            }

            // Render the speedometer
            classicRenderer.Render(yalmsCalculator.GetDisplayYalms());
        }

        // Toggle method to show or hide the window
        public void Toggle()
        {
            this.IsOpen = !this.IsOpen;
        }

        // Dispose method for cleanup if necessary
        public void Dispose()
        {
            // No unmanaged resources to clean up
        }

        // Get the calculator for debug window
        public YalmsCalculator GetCalculator()
        {
            return yalmsCalculator;
        }

        // Get the renderer for updating settings
        public ClassicRenderer GetRenderer()
        {
            return classicRenderer;
        }

        // Update damping from config
        public void UpdateDamping(float damping)
        {
            yalmsCalculator.SetDamping(damping);
        }

        // Update max speed from config
        public void UpdateMaxSpeed(float maxSpeed)
        {
            classicRenderer.SetMaxYalms(maxSpeed);
        }

        // Update redline from config
        public void UpdateRedlineStart(float redlineStart)
        {
            classicRenderer.SetRedlineStart(redlineStart);
        }
    }
}

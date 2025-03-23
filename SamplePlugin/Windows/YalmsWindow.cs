using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ZoomiesPlugin.Windows
{
    public class YalmsWindow : Window, IDisposable
    {
        // Helper classes for calculations and rendering
        private readonly YalmsCalculator yalmsCalculator;
        private readonly YalmsRenderer yalmsRenderer;

        // Constructor
        public YalmsWindow() : base("Yalms##YalmsWindow",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize)
        {
            // Set a default size for the window
            Size = new Vector2(350, 350);
            SizeCondition = ImGuiCond.FirstUseEver;

            // Get configuration
            var config = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // Create helper classes
            yalmsCalculator = new YalmsCalculator();
            yalmsRenderer = new YalmsRenderer();

            // Configure helpers from config
            yalmsCalculator.SetDamping(config.NeedleDamping);
            yalmsRenderer.SetMaxYalms(config.MaxYalms);
            yalmsRenderer.SetRedlineStart(config.RedlineStart);
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
            yalmsRenderer.Render(yalmsCalculator.GetDisplayYalms());

            // Check if close button was clicked
            if (yalmsRenderer.WasCloseButtonClicked())
            {
                this.Toggle();
            }
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
        public YalmsRenderer GetRenderer()
        {
            return yalmsRenderer;
        }
    }
}

using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ZoomiesPlugin.Windows
{
    public class NyanCatWindow : Window, IDisposable
    {
        // Helper classes for calculations and rendering
        private readonly YalmsCalculator yalmsCalculator;
        private readonly NyanCatRenderer nyanRenderer;

        // Constructor
        public NyanCatWindow() : base("NyanCat##NyanCatWindow",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize)
        {
            // Set a default size for the window
            Size = new Vector2(450, 150);
            SizeCondition = ImGuiCond.FirstUseEver;

            // Get configuration
            var config = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // Create helper classes
            yalmsCalculator = new YalmsCalculator();
            nyanRenderer = new NyanCatRenderer();

            // Configure helpers
            yalmsCalculator.SetDamping(config.NeedleDamping);
            nyanRenderer.SetMaxYalms(config.MaxYalms);
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

            // Render the Nyan Cat
            nyanRenderer.Render(yalmsCalculator.GetDisplayYalms());

            // Check if close button was clicked
            if (nyanRenderer.WasCloseButtonClicked())
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
            // Dispose of the nyan renderer's texture resources
            if (nyanRenderer != null)
            {
                nyanRenderer.Dispose();
            }
        }

        // Get the calculator for debug window
        public YalmsCalculator GetCalculator()
        {
            return yalmsCalculator;
        }

        // Get the renderer for updating settings
        public NyanCatRenderer GetRenderer()
        {
            return nyanRenderer;
        }
    }
}

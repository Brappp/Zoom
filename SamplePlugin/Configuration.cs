using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace ZoomiesPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        // UI settings
        public bool ShowSpeedometerOnStartup { get; set; } = true;

        // Speedometer type (0 = Classic, 1 = Nyan Cat, etc.)
        public int SelectedSpeedometerType { get; set; } = 0;

        // Speedometer settings
        public float MaxYalms { get; set; } = 20.0f;
        public float RedlineStart { get; set; } = 16.0f;
        public float NeedleDamping { get; set; } = 0.1f;

        // Save method to make saving less cumbersome
        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}

using System.Windows;

namespace WrightLauncher.Models
{
    public class PerformanceSettings
    {
        public bool EnablePerformanceMode { get; set; } = false;
        public bool EnableGPUAcceleration { get; set; } = true;
        public bool ReduceAnimations { get; set; } = false;
        public bool EnableImageCaching { get; set; } = true;
        public int MaxCacheSize { get; set; } = 100;
        public int AnimationFPS { get; set; } = 60;
    }
}



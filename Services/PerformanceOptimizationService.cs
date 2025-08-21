using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WrightLauncher.Services
{
    public static class PerformanceOptimizationService
    {
        public static void OptimizeWindow(Window window)
        {
            if (window == null) return;

            try
            {
                RenderOptions.SetBitmapScalingMode(window, BitmapScalingMode.HighQuality);
                RenderOptions.SetClearTypeHint(window, ClearTypeHint.Enabled);
                
                TextOptions.SetTextRenderingMode(window, TextRenderingMode.Auto);
                TextOptions.SetTextFormattingMode(window, TextFormattingMode.Ideal);
                
                RenderOptions.SetCachingHint(window, CachingHint.Cache);
                RenderOptions.SetCacheInvalidationThresholdMinimum(window, 0.5);
                RenderOptions.SetCacheInvalidationThresholdMaximum(window, 2.0);
                
                RenderOptions.SetEdgeMode(window, EdgeMode.Unspecified);
                
            }
            catch (Exception ex)
            {
            }
        }

        public static void OptimizeImageElement(FrameworkElement element)
        {
            if (element == null) return;

            try
            {
                RenderOptions.SetBitmapScalingMode(element, BitmapScalingMode.LowQuality);
                
                RenderOptions.SetCachingHint(element, CachingHint.Cache);
                
                element.UseLayoutRounding = true;
                element.SnapsToDevicePixels = true;
            }
            catch (Exception ex)
            {
            }
        }

        public static void CleanupMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                Converters.ChampionImageConverter.ClearCache();
                Converters.OptimizedImageConverter.ClearCache();
                
            }
            catch (Exception ex)
            {
            }
        }

        public static void SetPerformanceMode(bool enablePerformanceMode)
        {
            try
            {
                if (enablePerformanceMode)
                {
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}



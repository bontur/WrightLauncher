using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WrightLauncher.Services;

namespace WrightLauncher.Converters
{
    public class ChampionImageConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();
        private static readonly ConcurrentDictionary<string, bool> _loadingImages = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string championName && !string.IsNullOrEmpty(championName))
            {
                if (_imageCache.TryGetValue(championName, out var cachedImage))
                    return cachedImage;

                if (_loadingImages.ContainsKey(championName))
                    return CreatePlaceholderBitmap();

                _loadingImages.TryAdd(championName, true);
                _ = LoadImageAsync(championName);
                
                return CreatePlaceholderBitmap();
            }
            
            return CreatePlaceholderBitmap();
        }

        private static async Task LoadImageAsync(string championName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ChampionImageConverter] Starting to load image for: {championName}");
                
                var imageUrl = GetImageUrl(championName);
                System.Diagnostics.Debug.WriteLine($"[ChampionImageConverter] Image URL: {imageUrl}");
                
                var cachedBitmap = await ImageCacheService.Instance.GetCachedBitmapImageAsync(imageUrl);
                
                if (cachedBitmap != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ChampionImageConverter] Successfully loaded image for: {championName}");
                    
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _imageCache.TryAdd(championName, cachedBitmap);
                        
                        ImageCacheUpdated?.Invoke(championName);
                        System.Diagnostics.Debug.WriteLine($"[ChampionImageConverter] Triggered ImageCacheUpdated event for: {championName}");
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ChampionImageConverter] Failed to load image for: {championName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChampionImageConverter] Error loading image for {championName}: {ex.Message}");
            }
            finally
            {
                _loadingImages.TryRemove(championName, out _);
            }
        }

        private static string GetImageUrl(string championName)
        {
            return $"https://raw.communitydragon.org/latest/game/assets/characters/{championName.ToLower()}/hud/{championName.ToLower()}_square_0.png";
        }

        public static event Action<string>? ImageCacheUpdated;

        private BitmapImage CreatePlaceholderBitmap()
        {
            try
            {
                var transparentPngBytes = System.Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
                
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(transparentPngBytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return new BitmapImage();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static void ClearCache()
        {
            _imageCache.Clear();
        }
    }
}



using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using WrightLauncher.Services;

namespace WrightLauncher.Converters
{
    public class CachedImageConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string imageUrl && !string.IsNullOrEmpty(imageUrl))
            {
                if (_imageCache.TryGetValue(imageUrl, out var cachedImage))
                    return cachedImage;

                _ = LoadCachedImageAsync(imageUrl);
                
                return CreatePlaceholderImage();
            }
            
            return CreatePlaceholderImage();
        }

        private async Task LoadCachedImageAsync(string imageUrl)
        {
            try
            {
                if (_imageCache.ContainsKey(imageUrl))
                    return;

                var cachedBitmap = await ImageCacheService.Instance.GetCachedBitmapImageAsync(imageUrl);
                if (cachedBitmap != null)
                {
                    _imageCache.TryAdd(imageUrl, cachedBitmap);
                }
            }
            catch
            {
            }
        }

        private static BitmapImage CreatePlaceholderImage()
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.DecodePixelWidth = 1;
            bitmap.DecodePixelHeight = 1;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
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



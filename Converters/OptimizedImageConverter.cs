using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WrightLauncher.Services;

namespace WrightLauncher.Converters
{
    public class OptimizedImageConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string imageUrl || string.IsNullOrEmpty(imageUrl))
                return CreatePlaceholderImage();

            if (_imageCache.TryGetValue(imageUrl, out var cachedImage))
                return cachedImage;

            _ = LoadCachedImageAsync(imageUrl);
            
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
            var placeholder = new BitmapImage();
            placeholder.BeginInit();
            placeholder.DecodePixelWidth = 1;
            placeholder.DecodePixelHeight = 1;
            placeholder.EndInit();
            placeholder.Freeze();
            return placeholder;
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



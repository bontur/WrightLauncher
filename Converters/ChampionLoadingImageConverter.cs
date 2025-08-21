using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using WrightLauncher.Services;

namespace WrightLauncher.Converters
{
    public class ChampionLoadingImageConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();
        private static readonly ConcurrentDictionary<string, bool> _loadingImages = new();

        public static event Action<string>? ImageCacheUpdated;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string championName && !string.IsNullOrEmpty(championName))
            {
                DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Convert called for: {championName}", "DEBUG");
                
                if (_imageCache.TryGetValue(championName, out var cachedImage))
                {
                    DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Cache hit for: {championName}", "DEBUG");
                    return cachedImage;
                }

                if (_loadingImages.ContainsKey(championName))
                {
                    DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Already loading: {championName}", "DEBUG");
                    return CreatePlaceholderBitmap();
                }

                DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Starting async load for: {championName}", "DEBUG");
                
                _loadingImages.TryAdd(championName, true);
                _ = LoadImageAsync(championName);
                
                return CreatePlaceholderBitmap();
            }
            
            DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Invalid champion name: {value}", "ERROR");
            return CreatePlaceholderBitmap();
        }

        private static async Task LoadImageAsync(string championName)
        {
            try
            {
                DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Loading image for: {championName}", "DEBUG");
                
                var urlName = GetChampionUrlName(championName);
                
                var imageUrl = $"https://raw.githubusercontent.com/noxelisdev/LoL_DDragon/refs/heads/master/img/champion/loading/{urlName}_0.jpg";
                DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Image URL: {imageUrl}", "DEBUG");
                
                var cachedBitmap = await ImageCacheService.Instance.GetCachedBitmapImageAsync(imageUrl);
                
                if (cachedBitmap != null)
                {
                    DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Successfully loaded image for: {championName}", "SUCCESS");
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _imageCache.TryAdd(championName, cachedBitmap);
                        
                        ImageCacheUpdated?.Invoke(championName);
                        DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Triggered ImageCacheUpdated event for: {championName}", "SUCCESS");
                    });
                }
                else
                {
                    DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Failed to load image for: {championName}", "ERROR");
                }
            }
            catch (Exception ex)
            {
                DebugConsoleWindow.Instance?.WriteLine($"[ChampionLoadingImageConverter] Error loading image for {championName}: {ex.Message}", "ERROR");
            }
            finally
            {
                _loadingImages.TryRemove(championName, out _);
            }
        }

        private BitmapImage CreatePlaceholderBitmap()
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            
            var transparentPngBytes = System.Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
            bitmap.StreamSource = new MemoryStream(transparentPngBytes);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private static string GetChampionUrlName(string championName)
        {
            var urlNames = new Dictionary<string, string>
            {
                { "Wukong", "MonkeyKing" },
                { "Aurelion Sol", "AurelionSol" },
                { "Bel'Veth", "Belveth" },
                { "Kai'Sa", "Kaisa" },
                { "Cho'Gath", "Chogath" },
                { "Fiddlesticks", "Fiddlesticks" },
                { "Dr. Mundo", "DrMundo" },
                { "Jarvan IV", "JarvanIV" },
                { "K'Sante", "KSante" },
                { "Kha'Zix", "Khazix" },
                { "Kog'Maw", "KogMaw" },
                { "LeBlanc", "Leblanc" },
                { "Lee Sin", "LeeSin" },
                { "Master Yi", "MasterYi" },
                { "Miss Fortune", "MissFortune" },
                { "Nunu & Willump", "Nunu" },
                { "Rek'Sai", "RekSai" },
                { "Renata Glasc", "Renata" },
                { "Tahm Kench", "TahmKench" },
                { "Twisted Fate", "TwistedFate" },
                { "Vel'Koz", "Velkoz" },
                { "Xin Zhao", "XinZhao" }
            };
            
            if (urlNames.ContainsKey(championName))
                return urlNames[championName];
            
            return championName.Replace(" ", "").Replace("'", "").Replace(".", "");
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



using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace WrightLauncher.Services
{
    public class ImageCacheService
    {
        private static ImageCacheService? _instance;
        public static ImageCacheService Instance => _instance ??= new ImageCacheService();

        private readonly string _cacheDirectory;
        private readonly string _cacheIndexPath;
        private readonly HttpClient _httpClient;
        private Dictionary<string, string> _cacheIndex;

        private ImageCacheService()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _cacheDirectory = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "Cache");
            _cacheIndexPath = Path.Combine(_cacheDirectory, "cached.json");
            
            Directory.CreateDirectory(_cacheDirectory);
            
            DirectoryInfo cacheFolder = new DirectoryInfo(_cacheDirectory);
            cacheFolder.Attributes |= FileAttributes.Hidden;

            _cacheIndex = LoadCacheIndex();

            _httpClient = new HttpClient();
        }

        public async Task<string?> GetCachedImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return null;

            try
            {
                if (_cacheIndex.ContainsKey(imageUrl))
                {
                    var existingFileName = _cacheIndex[imageUrl];
                    var existingFilePath = Path.Combine(_cacheDirectory, existingFileName);
                    
                    if (File.Exists(existingFilePath))
                    {
                        return existingFilePath;
                    }
                    else
                    {
                        _cacheIndex.Remove(imageUrl);
                        SaveCacheIndex();
                    }
                }

                var fileName = GetHashFromUrl(imageUrl);
                var extension = GetFileExtension(imageUrl);
                var cachedFileName = $"{fileName}{extension}";
                var cachedFilePath = Path.Combine(_cacheDirectory, cachedFileName);

                var imageData = await _httpClient.GetByteArrayAsync(imageUrl);
                await File.WriteAllBytesAsync(cachedFilePath, imageData);

                _cacheIndex[imageUrl] = cachedFileName;
                SaveCacheIndex();
                
                return cachedFilePath;
            }
            catch
            {
                return null;
            }
        }

        public async Task<BitmapImage?> GetCachedBitmapImageAsync(string imageUrl)
        {
            try
            {
                var cachedFilePath = await GetCachedImageAsync(imageUrl);
                if (string.IsNullOrEmpty(cachedFilePath) || !File.Exists(cachedFilePath))
                    return null;

                return LoadBitmapImageFromFile(cachedFilePath);
            }
            catch
            {
                return null;
            }
        }

        private BitmapImage? LoadBitmapImageFromFile(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private Dictionary<string, string> LoadCacheIndex()
        {
            try
            {
                if (!File.Exists(_cacheIndexPath))
                {
                    return new Dictionary<string, string>();
                }

                var json = File.ReadAllText(_cacheIndexPath);
                var index = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                
                return index ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private void SaveCacheIndex()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_cacheIndex, Formatting.Indented);
                File.WriteAllText(_cacheIndexPath, json);
            }
            catch
            {
            }
        }

        private string GetHashFromUrl(string url)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
                return Convert.ToHexString(hash);
            }
        }

        private string GetFileExtension(string url)
        {
            try
            {
                var uri = new Uri(url);
                var extension = Path.GetExtension(uri.LocalPath);
                
                if (string.IsNullOrEmpty(extension) || !IsValidImageExtension(extension))
                {
                    return ".jpg";
                }
                
                return extension.ToLower();
            }
            catch
            {
                return ".jpg";
            }
        }

        private bool IsValidImageExtension(string extension)
        {
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            return Array.Exists(validExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        public void ClearCache()
        {
            try
            {
                if (Directory.Exists(_cacheDirectory))
                {
                    Directory.Delete(_cacheDirectory, true);
                    Directory.CreateDirectory(_cacheDirectory);
                    
                    DirectoryInfo cacheFolder = new DirectoryInfo(_cacheDirectory);
                    cacheFolder.Attributes |= FileAttributes.Hidden;
                }
            }
            catch
            {
            }
        }

        public long GetCacheSize()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                    return 0;

                var files = Directory.GetFiles(_cacheDirectory, "*", SearchOption.AllDirectories);
                long totalSize = 0;
                
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;
                }
                
                return totalSize;
            }
            catch
            {
                return 0;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}



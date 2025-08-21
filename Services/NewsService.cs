using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WrightLauncher.Models;
using WrightLauncher.Utilities;

namespace WrightLauncher.Services
{
    public class NewsService
    {
        private static NewsService? _instance;
        public static NewsService Instance => _instance ??= new NewsService();

        private readonly HttpClient _httpClient;
        private readonly string _dataUrl = $"{WrightUtils.E}/forLauncher/news?token={WrightUtils.D}";
        private List<News> _cachedNews = new();
        private DateTime _lastFetchTime = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

        private NewsService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<List<News>> GetNewsAsync()
        {
            try
            {
                if (_cachedNews.Count > 0 && DateTime.Now - _lastFetchTime < _cacheExpiry)
                {
                    return _cachedNews;
                }

                var response = await _httpClient.GetAsync(_dataUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var news = JsonSerializer.Deserialize<List<News>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (news != null)
                    {
                        _cachedNews = news;
                        _lastFetchTime = DateTime.Now;
                        return news;
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return _cachedNews;
        }

        public async Task RefreshNewsAsync()
        {
            _lastFetchTime = DateTime.MinValue;
            await GetNewsAsync();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}





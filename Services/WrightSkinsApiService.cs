using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WrightLauncher.Utilities;

namespace WrightLauncher.Services
{
    public static class WrightSkinsApiService
    {
        private static readonly HttpClient _httpClient;
        private static readonly string _cfg7 = WrightUtils.L;
        private static readonly string _cfg8 = WrightUtils.M;
        
        static WrightSkinsApiService()
        {
            _httpClient = new HttpClient();
            
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_cfg7}:{_cfg8}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        }
        
        public static async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await _httpClient.GetAsync(url);
        }
        
        public static async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
        {
            return await _httpClient.PostAsync(url, content);
        }
        
        public static async Task<string> GetStringAsync(string url)
        {
            return await _httpClient.GetStringAsync(url);
        }
    }
}




using Newtonsoft.Json;

namespace WrightLauncher.Models
{
    public class GitHubApiResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;
        
        [JsonProperty("download_url")]
        public string? DownloadUrl { get; set; }
    }
}


using Newtonsoft.Json;

namespace WrightLauncher.Models
{
    public class GitHubFileInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("path")]
        public string Path { get; set; } = "";

        [JsonProperty("type")]
        public string Type { get; set; } = "";

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("download_url")]
        public string DownloadUrl { get; set; } = "";

        [JsonProperty("url")]
        public string Url { get; set; } = "";

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; } = "";
    }
}


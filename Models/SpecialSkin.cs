using System;
using Newtonsoft.Json;

namespace WrightLauncher.Models
{
    public class SpecialSkin
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("targetID")]
        public string[] TargetID { get; set; } = new string[0];
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("author")]
        public string Author { get; set; } = string.Empty;
        
        [JsonProperty("CreatedBy")]
        public string CreatedBy { get; set; } = string.Empty;
        
        [JsonProperty("image_card")]
        public string ImageCard { get; set; } = string.Empty;
        
        [JsonProperty("image_preview")]
        public string ImagePreview { get; set; } = string.Empty;
        
        [JsonProperty("list_image")]
        public string ListImage { get; set; } = string.Empty;
        
        [JsonProperty("youtube_preview")]
        public string YoutubePreview { get; set; } = string.Empty;
        
        [JsonProperty("champion")]
        public string Champion { get; set; } = string.Empty;
        
        [JsonProperty("upload_date")]
        public string UploadDate { get; set; } = string.Empty;
        
        [JsonProperty("CreatedTime")]
        public string CreatedTime { get; set; } = string.Empty;
        
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;
        
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonProperty("isChampion")]
        public bool IsChampion { get; set; } = false;
        
        [JsonProperty("Downloads")]
        public string Downloads { get; set; } = "0";
        
        [JsonProperty("tags")]
        public string[] Tags { get; set; } = new string[0];
        
        [JsonProperty("nsfw")]
        public bool Nsfw { get; set; } = false;
        
        [JsonProperty("FileURL")]
        public string FileURL { get; set; } = string.Empty;
        
        [JsonProperty("download_url")]
        public string DownloadUrl { get; set; } = string.Empty;
        
        public string ActualDownloadUrl => !string.IsNullOrEmpty(FileURL) ? FileURL : DownloadUrl;
    }
}



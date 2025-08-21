using Newtonsoft.Json;
using System.Collections.Generic;

namespace WrightLauncher.Models
{
    public class Skin
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("author")]
        public string Author { get; set; } = string.Empty;
        
        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();
        
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonProperty("image_card")]
        public string ImageCard { get; set; } = string.Empty;
        
        [JsonProperty("list_image")]
        public string ListImage { get; set; } = string.Empty;
        
        [JsonProperty("image_preview")]
        public string ImagePreview { get; set; } = string.Empty;
        
        [JsonProperty("video_preview")]
        public string VideoPreview { get; set; } = string.Empty;
        
        [JsonProperty("youtube_preview")]
        public string YoutubePreview { get; set; } = string.Empty;
        
        [JsonProperty("champion")]
        public string Champion { get; set; } = string.Empty;
        
        [JsonProperty("FileURL")]
        public string FileURL { get; set; } = string.Empty;
        
        [JsonProperty("Downloads")]
        public string Downloads { get; set; } = "0";
        
        [JsonProperty("ApprovedBy")]
        public string ApprovedBy { get; set; } = string.Empty;
        
        [JsonProperty("ApprovedTime")]
        public string ApprovedTime { get; set; } = string.Empty;
        
        [JsonProperty("nsfw")]
        public bool Nsfw { get; set; } = false;
        
        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";
        
        [JsonProperty("isChampion")]
        public bool IsChampion { get; set; } = false;
        
        public DiscordUser? DiscordUser { get; set; }
        
        public string? CachedImageCard { get; set; }
        public string? CachedImagePreview { get; set; }
        public string? CachedDiscordAvatar { get; set; }
        
        public string ImagePath => ImageCard;
        public string WadURL => FileURL;
        public int ChampionId { get; set; }
        public string Rarity { get; set; } = "Common";
        public double Price { get; set; }
        public bool IsOwned { get; set; }
        public bool IsSelected { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}



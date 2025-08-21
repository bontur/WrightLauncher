using Newtonsoft.Json;
using System.Collections.Generic;

namespace WrightLauncher.Models
{
    public class ChampionSkinData
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("contentId")]
        public string ContentId { get; set; } = string.Empty;

        [JsonProperty("isBase")]
        public bool IsBase { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("skinClassification")]
        public string SkinClassification { get; set; } = string.Empty;

        [JsonProperty("splashPath")]
        public string SplashPath { get; set; } = string.Empty;

        [JsonProperty("uncenteredSplashPath")]
        public string UncenteredSplashPath { get; set; } = string.Empty;

        [JsonProperty("tilePath")]
        public string TilePath { get; set; } = string.Empty;

        [JsonProperty("loadScreenPath")]
        public string LoadScreenPath { get; set; } = string.Empty;

        [JsonProperty("rarity")]
        public string Rarity { get; set; } = string.Empty;

        [JsonProperty("isLegacy")]
        public bool IsLegacy { get; set; }

        [JsonProperty("chromaPath")]
        public string? ChromaPath { get; set; }

        [JsonProperty("chromas")]
        public List<ChromaData>? Chromas { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        public string CommunityDragonSplashUrl => ConvertToCommunityDragonUrl(SplashPath);
        public bool HasChromas => Chromas != null && Chromas.Count > 0;
        public string GitHubDownloadUrl { get; set; } = string.Empty;
        public string ChampionKey { get; set; } = string.Empty;
        public string VideoPreview { get; set; } = string.Empty;
        
        private string ConvertToCommunityDragonUrl(string splashPath)
        {
            if (string.IsNullOrEmpty(splashPath))
                return "";

var path = splashPath.ToLowerInvariant();
            
            if (path.StartsWith("/lol-game-data/assets/assets/"))
            {
                path = path.Substring("/lol-game-data/assets/assets/".Length);
            }

            return $"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/assets/{path}";
        }
    }

    public class ChromaData
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("chromaPath")]
        public string ChromaPath { get; set; } = string.Empty;

        [JsonProperty("colors")]
        public List<string> Colors { get; set; } = new();

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
    }

    public class CommunityDragonResponse
    {
        [JsonProperty("skins")]
        public List<ChampionSkinData> Skins { get; set; } = new();
    }

    public static class PathConverter
    {
        public static string ConvertToCommunityDragonUrl(string splashPath)
        {
            if (string.IsNullOrEmpty(splashPath))
                return "";

var path = splashPath.ToLowerInvariant();
            
            if (path.StartsWith("/lol-game-data/assets/assets/"))
            {
                path = path.Substring("/lol-game-data/assets/assets/".Length);
            }

            return $"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/assets/{path}";
        }
    }
}



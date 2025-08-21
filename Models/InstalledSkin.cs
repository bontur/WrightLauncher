using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WrightLauncher.Models
{
    public partial class InstalledSkin : ObservableObject
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("author")]
        public string Author { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("uptodate")]
        public bool UpToDate { get; set; } = true;

        [JsonProperty("installDate")]
        public DateTime InstallDate { get; set; } = DateTime.Now;

        [JsonProperty("champion")]
        public string Champion { get; set; } = string.Empty;

        [JsonProperty("imageCard")]
        public string ImageCard { get; set; } = "https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Zed_0.jpg";

        [JsonProperty("wadFile")]
        public string WadFile { get; set; } = string.Empty;

        [JsonProperty("isChampion")]
        public bool IsChampion { get; set; } = false;

        [JsonProperty("isBuilded")]
        public bool IsBuilded { get; set; } = false;

        [JsonProperty("isCustom")]
        public bool IsCustom { get; set; } = false;

        [JsonIgnore]
        public bool IsSpecial { get; set; } = false;

        [ObservableProperty]
        [JsonIgnore]
        private bool _isSelected = false;
    }
}



using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace WrightLauncher.Models
{
    public class Lobby
    {
        [JsonProperty("lobby_code")]
        public string LobbyCode { get; set; } = string.Empty;
        
        public string lobby_code => LobbyCode;
        
        [JsonProperty("lobby_id")]
        public int LobbyId { get; set; }
        
        [JsonProperty("lobby_creator_id")]
        public int LobbyCreatorId { get; set; }
        
        [JsonProperty("lobby_creator_username")]
        public string LobbyCreatorUsername { get; set; } = string.Empty;
        
        [JsonProperty("lobby_skins")]
        public ObservableCollection<LobbySkin> LobbySkins { get; set; } = new();
        
        [JsonProperty("lobby_members")]
        public List<int> LobbyMembers { get; set; } = new();
        
        [JsonProperty("everyoneCanUpload")]
        public bool EveryoneCanUpload { get; set; } = false;
        
        [JsonProperty("uploadPermissions")]
        public List<int> UploadPermissions { get; set; } = new();
        
        [JsonProperty("everyoneReady")]
        public bool EveryoneReady { get; set; } = false;
    }

    public partial class LobbySkin : ObservableObject
    {
        [JsonProperty("skin_id")]
        public string SkinId { get; set; } = string.Empty;
        
        [JsonProperty("skin_name")]
        public string SkinName { get; set; } = string.Empty;
        
        [JsonProperty("download_url")]
        public string DownloadUrl { get; set; } = string.Empty;
        
        [JsonProperty("champion_name")]
        public string ChampionName { get; set; } = string.Empty;
        
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;
        
        [JsonProperty("is_chroma")]
        public bool IsChroma { get; set; } = false;
        
        [JsonProperty("is_builded")]
        public bool IsBuilded { get; set; } = false;
        
        [JsonProperty("is_custom")]
        public bool IsCustom { get; set; } = false;
        
        [JsonProperty("image_card")]
        public string ImageCard { get; set; } = string.Empty;
        
        [JsonProperty("uploaded_by")]
        public int UploadedBy { get; set; }
        
        [JsonProperty("uploaded_by_username")]
        public string UploadedByUsername { get; set; } = string.Empty;
        
        [ObservableProperty]
        private bool _isSelected = false;
        
        [ObservableProperty]
        private bool _isDownloading = false;
        
        [ObservableProperty]
        private bool _isQueued = false;
        
        [ObservableProperty]
        private bool _isDownloaded = false;
        
        [ObservableProperty]
        private int _downloadProgress = 0;
        
        [ObservableProperty]
        private string _downloadProgressText = "";
        
        public void QueueForDownload()
        {
            IsQueued = true;
            IsDownloading = false;
            IsDownloaded = false;
            DownloadProgress = 0;
            DownloadProgressText = "Sıraya koyuldu";
        }
        
        public void StartDownload()
        {
            IsQueued = false;
            IsDownloading = true;
            IsDownloaded = false;
            DownloadProgress = 0;
            DownloadProgressText = "İndiriliyor %0";
        }
        
        public void UpdateDownloadProgress(int percentage)
        {
            if (percentage <= 0)
            {
                QueueForDownload();
                return;
            }
            
            if (!IsDownloading)
            {
                StartDownload();
            }
            
            DownloadProgress = percentage;
            DownloadProgressText = $"İndiriliyor %{percentage}";
            
            if (percentage >= 100)
            {
                CompleteDownload();
            }
        }
        
        public void CompleteDownload()
        {
            IsQueued = false;
            IsDownloading = false;
            IsDownloaded = true;
            DownloadProgress = 100;
            DownloadProgressText = "İndirildi";
        }
        
        public void ResetDownloadStatus()
        {
            IsQueued = false;
            IsDownloading = false;
            IsDownloaded = false;
            DownloadProgress = 0;
            DownloadProgressText = "";
        }
    }
}


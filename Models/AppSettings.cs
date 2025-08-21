namespace WrightLauncher.Models
{
    public class AppSettings
    {
        public string UserName { get; set; } = "Summoner";
        public string AvatarPath { get; set; } = "/Resources/Images/default_avatar.png";
        public string GamePath { get; set; } = string.Empty;
        public bool AutoLaunch { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;
        public string Theme { get; set; } = "Dark";
        public bool EnableAnimations { get; set; } = true;
        public double MasterVolume { get; set; } = 0.8;
        public string Language { get; set; } = "tr-TR";
        public int ItemsPerPage { get; set; } = 5;
        public string DownloadServer { get; set; } = "Github";
        public string AppVersion { get; set; } = "1.0.0";
        
        public DiscordSettings? Discord { get; set; }
        
        public PerformanceSettings Performance { get; set; } = new PerformanceSettings();
    }

    public class DiscordSettings
    {
        public string? EncryptedRefreshToken { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? GlobalName { get; set; }
        public string? AvatarHash { get; set; }
        public DateTime? LastAuthenticated { get; set; }
        public bool IsConnected { get; set; } = false;
    }
}



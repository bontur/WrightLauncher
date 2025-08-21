using Newtonsoft.Json;

namespace WrightLauncher.Models
{
    public class DiscordUser
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; } = string.Empty;
        
        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;
        
        [JsonProperty("discriminator")]
        public string Discriminator { get; set; } = string.Empty;
        
        [JsonProperty("avatar")]
        public DiscordAvatar Avatar { get; set; } = new();
        
        [JsonProperty("global_name")]
        public string? GlobalName { get; set; }
        
        [JsonProperty("accent_color")]
        public int AccentColor { get; set; }
        
        public string DisplayName 
        { 
            get 
            {
                if (!string.IsNullOrEmpty(GlobalName))
                    return GlobalName;
                    
                return !string.IsNullOrEmpty(Discriminator) && Discriminator != "0" 
                    ? $"{Username}#{Discriminator}" 
                    : Username;
            }
        }
        
        public string AvatarLink => Avatar?.Link ?? string.Empty;
    }
    
    public class DiscordAvatar
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("link")]
        public string Link { get; set; } = string.Empty;
        
        [JsonProperty("is_animated")]
        public bool IsAnimated { get; set; }
    }
}



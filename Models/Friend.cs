namespace WrightLauncher.Models
{
    public class Friend
    {
        public int Id { get; set; }
        
        public int UserID => Id;
        
        [Newtonsoft.Json.JsonProperty("discord_id")]
        public string DiscordId { get; set; } = string.Empty;
        
        public string Username { get; set; } = string.Empty;
        
        [Newtonsoft.Json.JsonProperty("avatar_url")]
        public string? AvatarUrl { get; set; }
    }

    public class FriendsResponse
    {
        public bool Success { get; set; }
        public List<Friend> Friends { get; set; } = new List<Friend>();
        public string? Error { get; set; }
    }
}


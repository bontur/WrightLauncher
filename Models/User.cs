using System.Collections.Generic;
using Newtonsoft.Json;

namespace WrightLauncher.Models
{
    public class User
    {
        [JsonProperty("user_id")]
        public int UserID { get; set; }

        [JsonProperty("discord_id")]
        public string DiscordID { get; set; } = string.Empty;

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("tokens")]
        public int Tokens { get; set; } = 0;

        [JsonProperty("friends")]
        public List<int> Friends { get; set; } = new List<int>();

        [JsonProperty("hashedtoken")]
        public string HashedToken { get; set; } = string.Empty;
    }
}



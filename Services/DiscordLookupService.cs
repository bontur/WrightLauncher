using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WrightLauncher.Models;

namespace WrightLauncher.Services
{
    public class DiscordLookupService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string DISCORD_LOOKUP_API = "https://discordlookup.mesalytic.moe/v1/user/";

        public static async Task<DiscordUser?> GetDiscordUserAsync(string discordId)
        {
            try
            {
                if (string.IsNullOrEmpty(discordId))
                    return null;

                var response = await httpClient.GetAsync($"{DISCORD_LOOKUP_API}{discordId}");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var jsonContent = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(jsonContent))
                    return null;

                var discordUser = JsonConvert.DeserializeObject<DiscordUser>(jsonContent);
                return discordUser;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}




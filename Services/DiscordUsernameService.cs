using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WrightLauncher.Services
{
    public class DiscordUsernameService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly Dictionary<int, DiscordUserInfo> _userCache = new Dictionary<int, DiscordUserInfo>();
        
        public static async Task<DiscordUserInfo> GetDiscordUserInfoAsync(int userId)
        {
            if (_userCache.TryGetValue(userId, out var cachedInfo))
            {
                return cachedInfo;
            }

            try
            {
                var discordId = await GetDiscordIdAsync(userId);
                if (string.IsNullOrEmpty(discordId))
                {
                    var fallbackInfo = new DiscordUserInfo
                    {
                        Username = $"User #{userId}",
                        AvatarUrl = null,
                        DiscordId = null
                    };
                    _userCache[userId] = fallbackInfo;
                    return fallbackInfo;
                }

                var discordInfo = await GetDiscordUserFromLookupAsync(discordId);
                if (discordInfo != null)
                {
                    _userCache[userId] = discordInfo;
                    return discordInfo;
                }

                var fallback = new DiscordUserInfo
                {
                    Username = $"User #{userId}",
                    AvatarUrl = null,
                    DiscordId = discordId
                };
                _userCache[userId] = fallback;
                return fallback;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });

                var errorInfo = new DiscordUserInfo
                {
                    Username = $"User #{userId}",
                    AvatarUrl = null,
                    DiscordId = null
                };
                return errorInfo;
            }
        }

        private static async Task<string> GetDiscordIdAsync(int userId)
        {
            try
            {
                var requestData = new { user_id = userId };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await WrightSkinsApiService.PostAsync("https://wrightskins.com/launcher/api/get-discord-id.php", content);
                var responseString = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<dynamic>(responseString);
                
                if (result?.success == true && result?.discord_id != null)
                {
                    return result.discord_id.ToString();
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return null;
            }
        }

        private static async Task<DiscordUserInfo> GetDiscordUserFromLookupAsync(string discordId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://discordlookup.mesalytic.moe/v1/user/{discordId}");
                var responseString = await response.Content.ReadAsStringAsync();

                var discordData = JsonConvert.DeserializeObject<dynamic>(responseString);
                
                if (discordData?.username != null)
                {
                    string avatarUrl = null;
                    if (discordData.avatar?.link != null)
                    {
                        avatarUrl = discordData.avatar.link.ToString();
                    }

                    return new DiscordUserInfo
                    {
                        Username = discordData.username.ToString(),
                        AvatarUrl = avatarUrl,
                        DiscordId = discordId
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return null;
            }
        }

        public static void ClearCache()
        {
            _userCache.Clear();
        }
    }

    public class DiscordUserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? DiscordId { get; set; }
    }
}


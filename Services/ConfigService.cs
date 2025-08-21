using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WrightLauncher.Models;

namespace WrightLauncher.Services
{
    public class ConfigService
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Riot Games", "League of Legends", "WrightSkins", "config.json");

        public static async Task<AppSettings> LoadConfigAsync()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    var defaultConfig = new AppSettings();
                    await SaveConfigAsync(defaultConfig);
                    return defaultConfig;
                }

                var jsonContent = await File.ReadAllTextAsync(ConfigPath);
                return JsonConvert.DeserializeObject<AppSettings>(jsonContent) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                return new AppSettings();
            }
        }

        public static async Task SaveConfigAsync(AppSettings config)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonContent = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(ConfigPath, jsonContent);
            }
            catch (Exception ex)
            {
            }
        }

        public static async Task SaveDiscordInfoAsync(DiscordUser user, string refreshToken)
        {
            try
            {
                var config = await LoadConfigAsync();
                
                config.Discord = new DiscordSettings
                {
                    EncryptedRefreshToken = EncryptionService.Encrypt(refreshToken),
                    UserId = user.Id,
                    Username = user.Username,
                    GlobalName = user.GlobalName,
                    AvatarHash = user.Avatar?.Id,
                    LastAuthenticated = DateTime.Now,
                    IsConnected = true
                };

                await SaveConfigAsync(config);
                
            }
            catch (Exception ex)
            {
            }
        }

        public static async Task<string?> GetDiscordRefreshTokenAsync()
        {
            try
            {
                var config = await LoadConfigAsync();
                
                if (config.Discord?.EncryptedRefreshToken != null)
                {
                    var decryptedToken = EncryptionService.Decrypt(config.Discord.EncryptedRefreshToken);
                    return string.IsNullOrEmpty(decryptedToken) ? null : decryptedToken;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static async Task<DiscordUser?> GetDiscordUserAsync()
        {
            try
            {
                var config = await LoadConfigAsync();
                
                if (config.Discord?.IsConnected == true && !string.IsNullOrEmpty(config.Discord.UserId))
                {
                    return new DiscordUser
                    {
                        Id = config.Discord.UserId,
                        Username = config.Discord.Username ?? "",
                        GlobalName = config.Discord.GlobalName,
                        Avatar = new DiscordAvatar
                        {
                            Id = config.Discord.AvatarHash ?? "",
                            Link = GetAvatarUrl(config.Discord.UserId, config.Discord.AvatarHash),
                            IsAnimated = config.Discord.AvatarHash?.StartsWith("a_") == true
                        }
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static async Task ClearDiscordInfoAsync()
        {
            try
            {
                var config = await LoadConfigAsync();
                config.Discord = new DiscordSettings { IsConnected = false };
                await SaveConfigAsync(config);
                
            }
            catch (Exception ex)
            {
            }
        }

        private static string GetAvatarUrl(string? userId, string? avatarHash)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(avatarHash))
            {
                return "https://cdn.discordapp.com/embed/avatars/0.png";
            }

            var extension = avatarHash.StartsWith("a_") ? "gif" : "png";
            return $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.{extension}?size=128";
        }
    }
}




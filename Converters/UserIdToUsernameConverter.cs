using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Newtonsoft.Json;
using WrightLauncher.Services;

namespace WrightLauncher.Converters
{
    public class UserIdToUsernameConverter : IValueConverter
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly ConcurrentDictionary<int, string> _usernameCache = new ConcurrentDictionary<int, string>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int userId && userId > 0)
            {
                
                if (_usernameCache.TryGetValue(userId, out string cachedUsername))
                {
                    return cachedUsername;
                }

                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    var currentUser = mainWindow.GetCurrentUser();
                    if (currentUser != null && currentUser.UserID == userId)
                    {
                        string username = currentUser.Username ?? $"User #{userId}";
                        _usernameCache.TryAdd(userId, username);
                        return username;
                    }
                }

                _ = Task.Run(async () => await FetchDiscordUsernameAsync(userId));
                
                return $"User #{userId}";
            }
            return "Unknown User";
        }

        private static async Task FetchDiscordUsernameAsync(int userId)
        {
            try
            {
                var discordUserInfo = await DiscordUsernameService.GetDiscordUserInfoAsync(userId);
                _usernameCache.TryAdd(userId, discordUserInfo.Username);
                
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        _ = Task.Run(async () => await mainWindow.RefreshCurrentLobbyAsync());
                    }
                });
                
            }
            catch (Exception ex)
            {
                _usernameCache.TryAdd(userId, $"User #{userId}");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WrightLauncher.Services;
using WrightLauncher.Models;

namespace WrightLauncher.Views
{
    public partial class LobbyInvitationPopup : UserControl
    {
        public string LobbyCode { get; private set; } = "";
        public string FromUserId { get; private set; } = "";
        public string FromUsername { get; private set; } = "";

        public event Action? OnAccept;
        public event Action? OnDecline;
        public event Action? OnClose;

        public LobbyInvitationPopup()
        {
            InitializeComponent();
        }

        private async Task<BitmapImage?> LoadBitmapImageWithTimeoutAsync(string url)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    
                    var imageData = await response.Content.ReadAsByteArrayAsync();
                    
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = new MemoryStream(imageData);
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void SetInvitationData(string lobbyCode, string fromUserId, string fromUsername, string lobbyCreator, int memberCount)
        {
            LobbyCode = lobbyCode;
            FromUserId = fromUserId;
            FromUsername = fromUsername;

            InviterNameText.Text = fromUsername;
            InviterAvatarText.Text = fromUsername.Length > 0 ? fromUsername[0].ToString().ToUpper() : "?";
            LobbyCodeText.Text = string.Format(LocalizationService.Instance.Translate("InvitePopupLobbyCode"), lobbyCode);
            LobbyCreatorText.Text = string.Format(LocalizationService.Instance.Translate("InvitePopupCreator"), lobbyCreator);
            LobbyMemberCountText.Text = string.Format(LocalizationService.Instance.Translate("InvitePopupMemberCount"), memberCount);

            LoadDiscordDataAsync(fromUserId);
        }

        private async void LoadDiscordDataAsync(string discordId)
        {
            try
            {
                if (!string.IsNullOrEmpty(discordId))
                {
                    var discordUser = await DiscordLookupService.GetDiscordUserAsync(discordId);
                    
                    if (discordUser != null)
                    {
                        if (!string.IsNullOrEmpty(discordUser.Username))
                        {
                            InviterNameText.Text = discordUser.DisplayName;
                        }

                        if (!string.IsNullOrEmpty(discordUser.AvatarLink))
                        {
                            try
                            {
                                var avatarBitmap = await LoadBitmapImageWithTimeoutAsync(discordUser.AvatarLink);
                                if (avatarBitmap != null)
                                {
                                    var avatarImage = new Image
                                    {
                                        Source = avatarBitmap,
                                        Stretch = System.Windows.Media.Stretch.UniformToFill,
                                        Width = 32,
                                        Height = 32
                                    };
                                    
                                    InviterAvatarBorder.Child = avatarImage;
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        public void ShowWithAnimation()
        {
            Visibility = Visibility.Visible;
            
            var border = (Border)Content;
            
            var transform = new System.Windows.Media.TranslateTransform(100, 50);
            border.RenderTransform = transform;
            
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            border.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            
            var slideX = new DoubleAnimation(100, 0, TimeSpan.FromMilliseconds(300));
            var slideY = new DoubleAnimation(50, 0, TimeSpan.FromMilliseconds(300));
            
            slideX.EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = EasingMode.EaseOut };
            slideY.EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = EasingMode.EaseOut };
            
            transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideX);
            transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideY);
        }

        public void HideWithAnimation()
        {
            var border = (Border)Content;
            var transform = border.RenderTransform as System.Windows.Media.TranslateTransform ?? new System.Windows.Media.TranslateTransform();
            border.RenderTransform = transform;
            
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            
            var slideOut = new DoubleAnimation(0, 100, TimeSpan.FromMilliseconds(200));
            slideOut.EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = EasingMode.EaseIn };
            
            fadeOut.Completed += (s, e) => Visibility = Visibility.Collapsed;
            
            border.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideOut);
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            OnAccept?.Invoke();
            HideWithAnimation();
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            OnDecline?.Invoke();
            HideWithAnimation();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            OnClose?.Invoke();
            HideWithAnimation();
        }
    }
}



using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WrightLauncher.Models;
using WrightLauncher.Services;

namespace WrightLauncher.Views
{
    public partial class LobbySettingsModal : Window
    {
        private Lobby _currentLobby;
        private SocketIORealtimeService _realtimeService;
        private ObservableCollection<LobbyMemberPermission> _memberPermissions;
        private ObservableCollection<LobbyMemberPermission> _kickableMembers;
        
        private static Dictionary<string, LobbyUIState> _lobbyUIStates = 
            new Dictionary<string, LobbyUIState>();

        public LobbySettingsModal(Lobby currentLobby, SocketIORealtimeService realtimeService)
        {
            InitializeComponent();
            _currentLobby = currentLobby;
            _realtimeService = realtimeService;

if (!_lobbyUIStates.TryGetValue(currentLobby.LobbyCode, out var uiState))
            {
                uiState = new LobbyUIState
                {
                    MemberPermissions = new ObservableCollection<LobbyMemberPermission>(),
                    EveryoneCanUpload = currentLobby.EveryoneCanUpload
                };
                _lobbyUIStates[currentLobby.LobbyCode] = uiState;
            }
            else
            {
            }
            
            _memberPermissions = uiState.MemberPermissions;
            _kickableMembers = new ObservableCollection<LobbyMemberPermission>();
            
            LobbyMembersItemsControl.ItemsSource = _memberPermissions;
            KickableMembersItemsControl.ItemsSource = _kickableMembers;
            
            LoadLobbySettings();
        }

        private void LoadLobbySettings()
        {
            var uiState = _lobbyUIStates[_currentLobby.LobbyCode];

EveryoneCanUploadCheckBox.IsChecked = uiState.EveryoneCanUpload;
            
            UpdateIndividualPermissionsVisibility();
            
            LoadLobbyMembers();
            LoadKickableMembers();
            
            this.UpdateLayout();
        }

        private async void LoadLobbyMembers()
        {
            
            if (_memberPermissions.Count > 0)
            {
                foreach (var member in _memberPermissions)
                {
                }
                return;
            }
            
            foreach (var memberId in _currentLobby.LobbyMembers)
            {
                if (memberId == _currentLobby.LobbyCreatorId)
                {
                    continue;
                }
                
                var discordUserInfo = await DiscordUsernameService.GetDiscordUserInfoAsync(memberId);
                
                var member = new LobbyMemberPermission
                {
                    UserId = memberId,
                    Username = discordUserInfo.Username,
                    AvatarUrl = discordUserInfo.AvatarUrl,
                    DiscordId = discordUserInfo.DiscordId,
                    CanUpload = _currentLobby.UploadPermissions.Contains(memberId)
                };
                
                _memberPermissions.Add(member);
            }
        }

        private async void LoadKickableMembers()
        {
            
            _kickableMembers.Clear();
            
            foreach (var memberId in _currentLobby.LobbyMembers)
            {
                if (memberId == _currentLobby.LobbyCreatorId)
                {
                    continue;
                }
                
                var discordUserInfo = await DiscordUsernameService.GetDiscordUserInfoAsync(memberId);
                
                var member = new LobbyMemberPermission
                {
                    UserId = memberId,
                    Username = discordUserInfo.Username,
                    AvatarUrl = discordUserInfo.AvatarUrl,
                    DiscordId = discordUserInfo.DiscordId,
                    CanUpload = false
                };
                
                _kickableMembers.Add(member);
            }
        }

        private void UpdateIndividualPermissionsVisibility()
        {
            IndividualPermissionsPanel.Visibility = EveryoneCanUploadCheckBox.IsChecked == true 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }

        private void EveryoneCanUploadCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_lobbyUIStates.TryGetValue(_currentLobby.LobbyCode, out var uiState))
            {
                uiState.EveryoneCanUpload = EveryoneCanUploadCheckBox.IsChecked == true;
            }
            
            UpdateIndividualPermissionsVisibility();
        }

        private async void KickMember(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int userId)
                {
                    var member = _kickableMembers.FirstOrDefault(m => m.UserId == userId);
                    if (member != null)
                    {
                        var result = MessageBox.Show(
                            string.Format(LocalizationService.Instance.Translate("LobbySettingKickConfirm"), member.Username),
                            LocalizationService.Instance.Translate("LobbySettingKickTitle"),
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            
                            await _realtimeService.KickLobbyMemberAsync(_currentLobby.LobbyCode, userId);
                            
                            _kickableMembers.Remove(member);
                            var permissionMember = _memberPermissions.FirstOrDefault(m => m.UserId == userId);
                            if (permissionMember != null)
                            {
                                _memberPermissions.Remove(permissionMember);
                            }
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.Translate("LobbySettingSaveError"), ex.Message), 
                    LocalizationService.Instance.Translate("LobbySettingErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MemberPermissionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is int userId)
            {
                var member = _memberPermissions.FirstOrDefault(m => m.UserId == userId);
                if (member != null)
                {
                    member.CanUpload = checkBox.IsChecked == true;
                }
            }
        }

        private async void SaveSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                var uiState = _lobbyUIStates[_currentLobby.LobbyCode];
                
                var settings = new LobbySettings
                {
                    LobbyCode = _currentLobby.LobbyCode,
                    EveryoneCanUpload = uiState.EveryoneCanUpload,
                    UploadPermissions = _memberPermissions
                        .Where(m => m.CanUpload)
                        .Select(m => m.UserId)
                        .ToList()
                };

await _realtimeService.UpdateLobbySettings(settings);
                
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.Translate("LobbySettingSaveError"), ex.Message), 
                    LocalizationService.Instance.Translate("LobbySettingErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseModal(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CloseModal(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }
        
        public static void ClearLobbyPermissions(string lobbyCode)
        {
            _lobbyUIStates.Remove(lobbyCode);
        }
    }

    public class LobbyMemberPermission : INotifyPropertyChanged
    {
        private bool _canUpload;
        
        public int UserId { get; set; }
        public string Username { get; set; }
        public string? AvatarUrl { get; set; }
        public string? DiscordId { get; set; }
        
        public bool CanUpload 
        { 
            get => _canUpload;
            set
            {
                if (_canUpload != value)
                {
                    _canUpload = value;
                    OnPropertyChanged(nameof(CanUpload));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LobbyUIState
    {
        public ObservableCollection<LobbyMemberPermission> MemberPermissions { get; set; } = new ObservableCollection<LobbyMemberPermission>();
        public bool EveryoneCanUpload { get; set; }
    }

    public class LobbySettings
    {
        public string LobbyCode { get; set; }
        public bool EveryoneCanUpload { get; set; }
        public List<int> UploadPermissions { get; set; }
    }
}



using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using WrightLauncher.Models;
using WrightLauncher.Services;

namespace WrightLauncher.Views
{
    public partial class SkinSelectionModal : Window
    {
        public ObservableCollection<InstalledSkin> InstalledSkins { get; set; } = new();
        public List<InstalledSkin> SelectedSkins => InstalledSkins.Where(s => s.IsSelected).ToList();
        public ObservableCollection<Skin> AllSkins { get; set; } = new();
        
        private readonly RealtimeService? _realtimeService;
        private readonly SocketIORealtimeService? _socketIOService;
        private List<InstalledSkin> _allInstalledSkins = new();

        public SkinSelectionModal(RealtimeService? realtimeService = null, SocketIORealtimeService? socketIOService = null)
        {
            InitializeComponent();
            DataContext = this;
            _realtimeService = realtimeService;
            _socketIOService = socketIOService;
            LoadInstalledSkins();
        }

        private async void LoadInstalledSkins()
        {
            try
            {
                await Task.Run(() =>
                {
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var installedJsonPath = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR", "installed.json");

                    if (File.Exists(installedJsonPath))
                    {
                        var jsonContent = File.ReadAllText(installedJsonPath);
                        var skins = JsonConvert.DeserializeObject<List<InstalledSkin>>(jsonContent);

                        if (skins != null)
                        {
                            skins.Reverse();
                            _allInstalledSkins = skins;
                            
                            Dispatcher.Invoke(() =>
                            {
                                foreach (var skin in skins)
                                {
                                    InstalledSkins.Add(skin);
                                }
                            });
                        }
                    }
                });

                LoadingPanel.Visibility = Visibility.Collapsed;
                SkinListPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                
                var errorStack = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                var errorText = new TextBlock
                {
                    Text = string.Format(LocalizationService.Instance.Translate("SkinSelectionError"), ex.Message),
                    FontSize = 14,
                    Foreground = Brushes.Red,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
                
                errorStack.Children.Add(errorText);
                
                var grid = new Grid();
                grid.Children.Add(errorStack);
                
                SkinListPanel.Content = grid;
                SkinListPanel.Visibility = Visibility.Visible;

            }
        }

        private void SkinItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && 
                element.DataContext is InstalledSkin skin)
            {
                skin.IsSelected = !skin.IsSelected;
            }
        }

        private async void AddSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            
            var selected = SelectedSkins;
            if (selected.Count == 0)
            {
                CustomMessageModal.ShowWarning(LocalizationService.Instance.Translate("SkinSelectionWarning"));
                return;
            }

var mainWindow = this.Owner as WrightLauncher.MainWindow;
            
            var currentUser = mainWindow?.GetCurrentUser();
            
            if (currentUser == null)
            {
                CustomMessageModal.ShowError(LocalizationService.Instance.Translate("SkinSelectionLoginRequired"));
                return;
            }
            
            AddSelectedButton.IsEnabled = false;
            AddSelectedButton.Content = LocalizationService.Instance.Translate("SkinSelectionAdding");

            try
            {
                int successCount = 0;
                int errorCount = 0;

                foreach (var skin in selected)
                {
                    var success = await AddSkinToLobby(skin, currentUser.UserID);
                    if (success)
                        successCount++;
                    else
                        errorCount++;
                }

                if (successCount > 0)
                {
                    await mainWindow?.RefreshCurrentLobbyAsync();
                    
                    var successMessage = string.Format(LocalizationService.Instance.Translate("SkinSelectionSuccess"), successCount);
                    if (errorCount > 0)
                    {
                        successMessage += string.Format(LocalizationService.Instance.Translate("SkinSelectionPartialError"), errorCount);
                    }
                    
                    CustomMessageModal.ShowSuccess(successMessage);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    CustomMessageModal.ShowError(LocalizationService.Instance.Translate("SkinSelectionFailure"));
                }
            }
            catch (Exception ex)
            {
                CustomMessageModal.ShowError(string.Format(LocalizationService.Instance.Translate("SkinSelectionAddError"), ex.Message));
            }
            finally
            {
                AddSelectedButton.IsEnabled = true;
                AddSelectedButton.Content = LocalizationService.Instance.Translate("SkinSelectionAddButton");
            }
        }

        private async Task<bool> AddSkinToLobby(InstalledSkin skin, int userId)
        {
            try
            {
                
                if (_socketIOService == null)
                {
                    return false;
                }
                
                var skinData = new
                {
                    skin_id = skin.Id.ToString(),
                    skin_name = !string.IsNullOrEmpty(skin.Name) ? skin.Name : "Unknown Skin",
                    champion_name = !string.IsNullOrEmpty(skin.Champion) ? skin.Champion : "Unknown",
                    version = !string.IsNullOrEmpty(skin.Version) ? skin.Version : "1.0",
                    is_builded = skin.IsBuilded,
                    is_custom = skin.IsCustom,
                    image_card = !string.IsNullOrEmpty(skin.ImageCard) ? skin.ImageCard : "",
                    download_url = ""
                };

                bool success = await _socketIOService.AddSkinToLobbyAsync(userId.ToString(), skinData);
                
                if (success)
                {
                }
                else
                {
                }
                
                return success;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterSkins();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            FilterSkins();
        }

        private void FilterSkins()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            
            InstalledSkins.Clear();
            
            var filteredSkins = string.IsNullOrEmpty(searchText) 
                ? _allInstalledSkins 
                : _allInstalledSkins.Where(skin => 
                    skin.Name.ToLower().Contains(searchText) || 
                    skin.Champion.ToLower().Contains(searchText) ||
                    (skin.IsCustom && "custom".Contains(searchText)) ||
                    (skin.IsBuilded && "builded".Contains(searchText)) ||
                    (skin.IsCustom && LocalizationService.Instance.Translate("search_custom").ToLower().Contains(searchText)) ||
                    (skin.IsBuilded && LocalizationService.Instance.Translate("search_builded").ToLower().Contains(searchText))
                ).ToList();
            
            foreach (var skin in filteredSkins)
            {
                InstalledSkins.Add(skin);
            }
        }
    }
}



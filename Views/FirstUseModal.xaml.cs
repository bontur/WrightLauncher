using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using WrightLauncher.Services;

namespace WrightLauncher.Views
{
    public partial class FirstUseModal : Window
    {
        private static readonly string[] ChampionPairs = new[]
        {
            "Aatrox",
            "Lucian", 
            "Qiyana",
            "Zed"
        };

        public FirstUseModal()
        {
            InitializeComponent();
            
            SetRandomChampion();
            
            LoadCurrentSettings();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeInStoryboard = (Storyboard)FindResource("FadeInStoryboard");
            fadeInStoryboard.Begin();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void FadeOutStoryboard_Completed(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CloseModalWithAnimation()
        {
            var fadeOutStoryboard = (Storyboard)FindResource("FadeOutStoryboard");
            fadeOutStoryboard.Begin();
        }

        private void SetRandomChampion()
        {
            try
            {
                var random = new Random();
                var selectedChampion = ChampionPairs[random.Next(ChampionPairs.Length)];
                
                if (ChampionImage != null)
                {
                    ChampionImage.Source = new BitmapImage(new Uri($"pack://application:,,,/Assets/ADV/{selectedChampion}.png"));
                }
                
                if (BackgroundGrid?.Background is ImageBrush imageBrush)
                {
                    imageBrush.ImageSource = new BitmapImage(new Uri($"pack://application:,,,/Assets/ADV/{selectedChampion}Bg.png"));
                }
            }
            catch (Exception)
            {
            }
        }

        private void LoadCurrentSettings()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(configPath))
                {
                    string configContent = File.ReadAllText(configPath);
                    dynamic config = JsonConvert.DeserializeObject(configContent);
                    if (config?.GamePath != null)
                    {
                        GamePathTextBox.Text = config.GamePath.ToString();
                    }
                }

                string langPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang.json");
                if (File.Exists(langPath))
                {
                    string langContent = File.ReadAllText(langPath);
                    dynamic langConfig = JsonConvert.DeserializeObject(langContent);
                    if (langConfig?.Language != null)
                    {
                        string currentLang = langConfig.Language.ToString();
                        
                        foreach (ComboBoxItem item in LanguageComboBox.Items)
                        {
                            if (item.Tag.ToString() == currentLang)
                            {
                                LanguageComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }

                if (LanguageComboBox.SelectedItem == null)
                {
                    LanguageComboBox.SelectedIndex = 1;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void BrowseGamePathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "League of Legends|LeagueClient.exe",
                    Title = "Select League of Legends Folder",
                    InitialDirectory = @"C:\Riot Games\League of Legends"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedPath = openFileDialog.FileName;
                    string gamePath = Path.GetDirectoryName(selectedPath);
                    
                    if (Path.GetFileName(gamePath) == "League of Legends")
                    {
                        gamePath = Path.Combine(gamePath, "Game");
                    }
                    
                    GamePathTextBox.Text = gamePath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while selecting the file: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool hasChanges = false;

                if (!string.IsNullOrEmpty(GamePathTextBox.Text))
                {
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string wrightSkinsPath = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
                    
                    if (!Directory.Exists(wrightSkinsPath))
                    {
                        Directory.CreateDirectory(wrightSkinsPath);
                    }
                    
                    string configPath = Path.Combine(wrightSkinsPath, "config.json");
                    
                    if (File.Exists(configPath))
                    {
                        string configContent = File.ReadAllText(configPath);
                        var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(configContent) ?? new Dictionary<string, object>();
                        
                        config["GamePath"] = GamePathTextBox.Text;
                        File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
                    }
                    else
                    {
                        var newConfig = new { GamePath = GamePathTextBox.Text };
                        File.WriteAllText(configPath, JsonConvert.SerializeObject(newConfig, Formatting.Indented));
                    }
                    
                    string dataPath = Path.Combine(wrightSkinsPath, "data.json");
                    var data = new { firstSettingsDone = true };
                    string dataJson = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(dataPath, dataJson);
                    
                    hasChanges = true;
                }

                if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
                {
                    string selectedLang = selectedItem.Tag.ToString()!;
                    
                    string languageName = selectedItem.Content.ToString()!;
                    LanguageSettingsService.Instance.SaveLanguageSettings(selectedLang, languageName);
                    
                    LocalizationService.Instance.Load(selectedLang);
                    
                    hasChanges = true;
                }

                if (hasChanges)
                {
                }

                CloseModalWithAnimation();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while saving settings: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            CloseModalWithAnimation();
        }
    }
}



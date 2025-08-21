using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WrightLauncher.Models;
using WrightLauncher.Commands;
using WrightLauncher.Services;

namespace WrightLauncher.Views
{
    public partial class WrightAdvModal : Window, INotifyPropertyChanged
    {
        public ICommand JoinDiscordCommand { get; }
        private readonly DiscordUser? _currentDiscordUser;
        
        public static event Action? DiscordConnectionChanged;

        private static readonly string[] ChampionPairs = new[]
        {
            "Aatrox",
            "Lucian", 
            "Qiyana",
            "Zed"
        };

        private bool _isDiscordConnected;
        public bool IsDiscordConnected
        {
            get => _isDiscordConnected;
            set
            {
                _isDiscordConnected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public WrightAdvModal(DiscordUser? currentDiscordUser = null)
        {
            InitializeComponent();
            DataContext = this;
            
            _currentDiscordUser = currentDiscordUser;
            
            JoinDiscordCommand = new RelayCommand(JoinDiscord);
            
            SetRandomChampion();
            
            UpdateDiscordUserInfo();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeInStoryboard = (Storyboard)FindResource("FadeInStoryboard");
            fadeInStoryboard.Begin();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseModalWithAnimation();
            }
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == sender)
            {
                CloseModalWithAnimation();
            }
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

        private void UpdateDiscordUserInfo()
        {
            try
            {
                if (_currentDiscordUser == null)
                {
                    IsDiscordConnected = false;
                    if (AdvModalDiscordUsername != null)
                        AdvModalDiscordUsername.Text = LocalizationService.Instance.Translate("ADVNotConnected");
                }
                else
                {
                    IsDiscordConnected = true;
                    if (AdvModalDiscordUsername != null)
                        AdvModalDiscordUsername.Text = _currentDiscordUser.DisplayName;
                }
            }
            catch (Exception)
            {
                if (AdvModalDiscordUsername != null)
                    AdvModalDiscordUsername.Text = LocalizationService.Instance.Translate("ADVDiscordUser");
            }
        }

        private void CloseModalButton_Click(object sender, RoutedEventArgs e)
        {
            CloseModalWithAnimation();
        }

        private void AdvModalDisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.DisconnectDiscord();
                    
                    CloseModalWithAnimation();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LocalizationService.Instance.Translate("ADVDiscordConnectionError"), ex.Message), 
                    LocalizationService.Instance.Translate("ErrorModalTitle"), 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void AdvModalConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    CloseModalWithAnimation();
                    
                    mainWindow.Activate();
                    mainWindow.OpenSettingsPage();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LocalizationService.Instance.Translate("ADVSettingsPageError"), ex.Message), 
                    LocalizationService.Instance.Translate("ErrorModalTitle"), 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private async void AdvModalKontrolEtButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null && _currentDiscordUser != null && button != null)
                {
                    button.IsEnabled = false;
                    button.Content = LocalizationService.Instance.Translate("ADVChecking");
                    
                    await mainWindow.CheckAndCacheGuildMembership(_currentDiscordUser.Id);
                    
                    bool isInGuild = mainWindow.IsInWrightGuild;
                    
                    if (isInGuild)
                    {
                        CloseModalWithAnimation();
                        
                        mainWindow.Activate();
                        mainWindow.OpenLobbyPage();
                    }
                    else
                    {
                        MessageBox.Show(
                            LocalizationService.Instance.Translate("ADVServerMembershipMessage"), 
                            LocalizationService.Instance.Translate("ADVServerMembershipRequired"), 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LocalizationService.Instance.Translate("ADVCheckError"), ex.Message), 
                    LocalizationService.Instance.Translate("ErrorModalTitle"), 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
            finally
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = IsDiscordConnected;
                    button.Content = LocalizationService.Instance.Translate("ADVCheckButton");
                }
            }
        }

        private void JoinDiscord()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://bontur.com.tr/discord",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(LocalizationService.Instance.Translate("ADVDiscordLinkError"), ex.Message), 
                    LocalizationService.Instance.Translate("ErrorModalTitle"), 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}




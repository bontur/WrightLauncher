using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace WrightLauncher
{
    public partial class SplashWindow : Window
    {
        private readonly MainWindow _mainWindow;
        
        public SplashWindow()
        {
            InitializeComponent();
            
            _mainWindow = new MainWindow();
            
            Loaded += OnLoaded;
        }
        
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await SimulateLoading();
            
            await ShowMainWindow();
        }
        
        private async Task SimulateLoading()
        {
            await Task.Delay(2500);
            
        }
        
        private async Task ShowMainWindow()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseIn }
            };
            
            fadeOut.Completed += (s, e) =>
            {
                _mainWindow.Show();
                
                this.Close();
            };
            
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}



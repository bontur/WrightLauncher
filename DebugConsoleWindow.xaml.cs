using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WrightLauncher
{
    public partial class DebugConsoleWindow : Window
    {
        private static DebugConsoleWindow? _instance;
        private readonly List<string> _logMessages = new();
        
        private static readonly bool HIDE_DEBUG_CONSOLE = true;

        public static DebugConsoleWindow Instance
        {
            get
            {
                if (_instance == null || !_instance.IsLoaded)
                {
                    _instance = new DebugConsoleWindow();
                }
                return _instance;
            }
        }

        public DebugConsoleWindow()
        {
            InitializeComponent();
            Title = "Debug Console";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = SystemParameters.PrimaryScreenWidth - Width - 50;
            Top = 50;
            
            if (HIDE_DEBUG_CONSOLE)
            {
                this.Visibility = Visibility.Hidden;
                this.WindowState = WindowState.Minimized;
            }
        }

        public static void ToggleVisibility()
        {
            var console = Instance;
            if (console.Visibility == Visibility.Hidden)
            {
                console.Show();
                console.Visibility = Visibility.Visible;
            }
            else
            {
                console.Hide();
                console.Visibility = Visibility.Hidden;
            }
        }

        public void WriteLine(string message, string level = "INFO")
        {
            if (HIDE_DEBUG_CONSOLE) return;
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] [{level}] {message}";
            
            Dispatcher.Invoke(() =>
            {
                _logMessages.Add(logMessage);
                
                if (_logMessages.Count > 1000)
                {
                    _logMessages.RemoveAt(0);
                }
                
                LogTextBox.AppendText(logMessage + Environment.NewLine);
                LogTextBox.ScrollToEnd();
                
                SetTextColor(level);
            });
        }

        private void SetTextColor(string level)
        {
            var brush = level switch
            {
                "ERROR" => Brushes.Red,
                "WARN" => Brushes.Orange,
                "SUCCESS" => Brushes.LimeGreen,
                "API" => Brushes.Cyan,
                _ => Brushes.White
            };
            
            LogTextBox.Foreground = brush;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            _logMessages.Clear();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"WrightLauncher_Debug_{timestamp}.log";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                
                System.IO.File.WriteAllLines(filePath, _logMessages);
                WriteLine($"Debug log kaydedildi: {filePath}", "SUCCESS");
            }
            catch (Exception ex)
            {
                WriteLine($"Log kaydetme hatası: {ex.Message}", "ERROR");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}



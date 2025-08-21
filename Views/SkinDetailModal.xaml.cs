using WrightLauncher.Models;
using System.Windows;

namespace WrightLauncher.Views
{
    public partial class SkinDetailModal : Window
    {
        public Skin? SelectedSkin { get; set; }

        public SkinDetailModal()
        {
            InitializeComponent();
        }

        public SkinDetailModal(Skin skin) : this()
        {
            SelectedSkin = skin;
            DataContext = skin;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSkin != null)
            {
                MessageBox.Show($"{SelectedSkin.Name} is downloading...", "Download", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSkin != null)
            {
                MessageBox.Show($"{SelectedSkin.Name} preview is coming soon!", "Preview", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class BoolToColorConverter : System.Windows.Data.IValueConverter
    {
        public static readonly BoolToColorConverter Instance = new BoolToColorConverter();

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isOwned)
            {
                return isOwned ? "#22C55E" : "#EF4444";
            }
            return "#94A3B8";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    public class BoolToStatusConverter : System.Windows.Data.IValueConverter
    {
        public static readonly BoolToStatusConverter Instance = new BoolToStatusConverter();

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isOwned)
            {
                return isOwned ? "OWNED" : "NOT OWNED";
            }
            return "UNKNOWN";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    public class TagsToStringConverter : System.Windows.Data.IValueConverter
    {
        public static readonly TagsToStringConverter Instance = new TagsToStringConverter();

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is System.Collections.Generic.List<string> tags && tags.Count > 0)
            {
                return string.Join(", ", tags);
            }
            return "No Tags";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new BoolToVisibilityConverter();

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}




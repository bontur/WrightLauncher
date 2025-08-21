using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WrightLauncher.Models;

namespace WrightLauncher.Converters
{
    public class SkinImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Skin skin)
            {
                if (!string.IsNullOrEmpty(skin.ImagePreview))
                    return skin.ImagePreview;
                    
                return skin.ImageCard;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HasYouTubePreviewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Skin skin && !string.IsNullOrEmpty(skin.YoutubePreview))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NoYouTubePreviewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Skin skin && !string.IsNullOrEmpty(skin.YoutubePreview))
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}




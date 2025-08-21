using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WrightLauncher.Converters
{
    public class HexColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
            {
                try
                {
                    if (hexColor.StartsWith("#"))
                    {
                        return (Color)ColorConverter.ConvertFromString(hexColor);
                    }
                    else
                    {
                        return (Color)ColorConverter.ConvertFromString("#" + hexColor);
                    }
                }
                catch
                {
                    return (Color)ColorConverter.ConvertFromString("#6366f1");
                }
            }
            
            return (Color)ColorConverter.ConvertFromString("#6366f1");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return color.ToString();
            }
            return "#6366f1";
        }
    }
}



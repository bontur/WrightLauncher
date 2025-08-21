using System;
using System.Globalization;
using System.Windows.Data;
using WrightLauncher.Services;

namespace WrightLauncher.Converters
{
    public class ChampionSkinsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string championName)
            {
                var template = LocalizationService.Instance.Translate("ChampionSkins");
                return string.Format(template, championName);
            }
            return value;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


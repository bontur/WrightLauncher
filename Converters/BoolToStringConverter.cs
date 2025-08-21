using System.Globalization;
using System.Windows.Data;
using WrightLauncher.Services;

namespace WrightLauncher.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public static BoolToStringConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? LocalizationService.Instance.Translate("owns_skin") : LocalizationService.Instance.Translate("not_owns_skin");
            }
            return LocalizationService.Instance.Translate("unknown_status");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


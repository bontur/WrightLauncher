using System.Globalization;
using System.Windows.Data;

namespace WrightLauncher.Converters
{
    public class LobbyCodeMaskConverter : IValueConverter
    {
        public static LobbyCodeMaskConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string lobbyCode && !string.IsNullOrEmpty(lobbyCode))
            {
                if (lobbyCode.Length >= 6)
                {
                    return $"WRIGHT-{new string('*', lobbyCode.Length - 6)}";
                }
                return $"WRIGHT-{new string('*', lobbyCode.Length)}";
            }
            return "WRIGHT-*******";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


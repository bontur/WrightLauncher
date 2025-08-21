using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WrightLauncher.Converters
{
    public class IsCurrentUserLobbyCreatorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && 
                values[0] is int lobbyCreatorId && 
                values[1] is int currentUserId)
            {
                return lobbyCreatorId == currentUserId ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WrightLauncher.Converters
{
    public class BuildedButNotCustomVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is bool isBuilded && values[1] is bool isCustom)
            {
                return (isBuilded && !isCustom) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

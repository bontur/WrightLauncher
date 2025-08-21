using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using WrightLauncher.Services;

namespace WrightLauncher.Converters
{
    public class LocExtension : MarkupExtension
    {
        public string Key { get; set; }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding("CurrentLanguage")
            {
                Source = LocalizationService.Instance,
                Converter = new LocalizationConverter(),
                ConverterParameter = Key,
                Mode = BindingMode.OneWay
            };

            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target)
            {
                if (target.TargetObject is DependencyObject && target.TargetProperty is DependencyProperty)
                {
                    return binding.ProvideValue(serviceProvider);
                }
            }

            return LocalizationService.Instance.Translate(Key);
        }
    }

    public class LocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter is string key)
            {
                return LocalizationService.Instance.Translate(key);
            }
            return parameter?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}



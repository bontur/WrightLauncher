using System.Globalization;
using System.Windows.Data;
using WrightLauncher.Models;
using System.Linq;

namespace WrightLauncher.Converters
{
    public class InstalledSkinImageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 3 && values[2] is string imageCard && !string.IsNullOrEmpty(imageCard))
            {
                return imageCard;
            }
            
            if (values.Length >= 2 && values[0] is int skinId && values[1] is IEnumerable<Skin> allSkins)
            {
                var matchingSkin = allSkins.FirstOrDefault(s => s.Id == skinId);
                if (matchingSkin != null)
                {
                    if (!string.IsNullOrEmpty(matchingSkin.ImagePreview))
                        return matchingSkin.ImagePreview;
                        
                    return matchingSkin.ImageCard;
                }
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace InstantMessaging.Converters
{
    class FromMeBoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool? b = (bool?)value;
            if (b ?? false)
            {
                return new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColorLight3"]);
            }
            return new SolidColorBrush(Windows.UI.Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

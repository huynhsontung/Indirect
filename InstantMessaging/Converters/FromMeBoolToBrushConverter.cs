using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace InstantMessaging.Converters
{
    class FromMeBoolToBrushConverter : IValueConverter
    {
        public Brush FromMe { get; set; }
        public Brush FromThem { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool? b = (bool?)value;
            if (b ?? false)
            {
                return FromMe;
            }

            return FromThem;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

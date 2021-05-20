using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    class BooleanVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                if (Invert)
                    return b ? Visibility.Collapsed : Visibility.Visible;
                return b ? Visibility.Visible : Visibility.Collapsed;
            }

            return Invert ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

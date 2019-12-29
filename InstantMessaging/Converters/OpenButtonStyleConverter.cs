using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace InstantMessaging.Converters
{
    class OpenButtonStyleConverter : IValueConverter
    {
        public Style AvailableStyle { get; set; }
        public Style NotAvailableStyle { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var b = (bool) value;
            return b ? NotAvailableStyle : AvailableStyle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
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

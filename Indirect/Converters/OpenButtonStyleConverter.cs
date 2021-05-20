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
            if (value is bool b)
            {

                return b ? NotAvailableStyle : AvailableStyle;
            }

            return AvailableStyle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

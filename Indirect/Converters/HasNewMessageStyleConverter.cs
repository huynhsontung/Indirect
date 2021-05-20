using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    public class HasNewMessageStyleConverter : IValueConverter
    {
        public Style NothingNew { get; set; }
        public Style HasNewMessage { get; set; }

        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return b ? HasNewMessage : NothingNew;
            }

            return NothingNew;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

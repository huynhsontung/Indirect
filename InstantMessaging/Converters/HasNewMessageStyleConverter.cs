using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace InstantMessaging.Converters
{
    public class HasNewMessageStyleConverter : IValueConverter
    {
        public Style NothingNew { get; set; }
        public Style HasNewMessage { get; set; }

        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            bool? b = (bool?)value;
            if (b ?? false)
            {
                return HasNewMessage;
            }
            return NothingNew;

        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

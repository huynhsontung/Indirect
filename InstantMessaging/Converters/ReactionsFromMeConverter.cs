using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using InstantMessaging.Wrapper;

namespace InstantMessaging.Converters
{
    class ReactionsFromMeConverter : IValueConverter
    {
        public Style FromMe { get; set; }
        public Style FromThem { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var b = (bool?) value;
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

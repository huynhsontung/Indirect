using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    class SendButtonGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var s = (string) value;
            return string.IsNullOrEmpty(s) ? "\xEB51" : "\xE724";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

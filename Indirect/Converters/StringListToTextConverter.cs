using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    class StringListToTextConverter : IValueConverter
    {
        public string Delimiter { get; set; } = Environment.NewLine;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var list = (IEnumerable<string>) value;
            return string.Join(Delimiter, list);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

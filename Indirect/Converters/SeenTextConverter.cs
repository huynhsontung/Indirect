using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    class SeenTextConverter : IValueConverter
    {
        private static readonly StringListToTextConverter ListConverter = new StringListToTextConverter{Delimiter = ", "};

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var list = (IList<string>) value;
            if (list.Count == 0)
            {
                return "Seen";
            }

            if (list.Count == 1 && list[0] == "@everyone")
            {
                return "Seen by everyone";
            }
            if (list.Count <= 3)
            {
                return "Seen by " + ListConverter.Convert(list, typeof(IList<string>), null, "");
            }

            return $"Seen by {list[0]}, {list[1]}, {list[2]} and {list.Count - 3} others";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

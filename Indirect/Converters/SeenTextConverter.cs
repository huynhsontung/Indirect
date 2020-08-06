using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    class SeenTextConverter : IValueConverter
    {
        private static StringListToTextConverter _listConverter = new StringListToTextConverter{Delimiter = ", "};

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var list = (ObservableCollection<string>) value;
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
                return "Seen by " + _listConverter.Convert(list, typeof(ObservableCollection<string>), null, "");
            }

            return $"Seen by {list[0]}, {list[1]}, {list[2]} and {list.Count - 3} other";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    class VariableMaxWidthConverter : IValueConverter
    {
        public double MaxLengthSingle { get; set; } = 300;
        public double MaxLengthMultiple { get; set; } = 220;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var length = (int) value;
            return length > 42 ? MaxLengthMultiple : MaxLengthSingle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    public class EqualityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public double ReferenceValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (Invert)
                return ReferenceValue != System.Convert.ToDouble(value);
            return ReferenceValue == System.Convert.ToDouble(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
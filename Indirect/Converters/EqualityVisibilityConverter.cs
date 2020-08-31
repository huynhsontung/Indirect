using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    class EqualityVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public double ReferenceValue { get; set; } = 0;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var number = System.Convert.ToDouble(value);
            if (!Invert)
                return number == ReferenceValue ? Visibility.Visible : Visibility.Collapsed;
            return number == ReferenceValue ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
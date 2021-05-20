using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    public class FromMeBoolToAlignmentConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return b ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }

            return HorizontalAlignment.Left;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

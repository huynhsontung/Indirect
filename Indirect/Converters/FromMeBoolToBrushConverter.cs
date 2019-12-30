using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Indirect.Converters
{
    class FromMeBoolToBrushConverter : IValueConverter
    {
        public Brush FromMe { get; set; }
        public Brush FromThem { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool? b = (bool?)value;
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

using System;
using System.Diagnostics;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    public class HumanizedDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime target;
            switch (value)
            {
                case DateTimeOffset dateTimeOffset:
                    target = dateTimeOffset.LocalDateTime;
                    break;
                case DateTime time:
                    target = time;
                    break;
                default:
                    Debug.WriteLine($"{nameof(HumanizedDateTimeConverter)}: Value is neither DateTimeOffset or DateTime. Return empty string.");
                    return string.Empty;
            }

            var startOfDay = target.Date;

            if (startOfDay == DateTime.Now.Date)
            {
                return $"Today {target:t}";
            } 
            if (startOfDay == DateTime.Now.Date.AddDays(-1))
            {
                return $"Yesterday {target:t}";
            }

            return $"{target:g}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
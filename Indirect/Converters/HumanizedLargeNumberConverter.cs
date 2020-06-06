using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Indirect.Converters
{
    class HumanizedLargeNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var number = System.Convert.ToDouble(value);
            if (number == 0) return "0";
            int mag = (int)(Math.Floor(Math.Log10(number)) / 3); // Truncates to 6, divides to 2
            double divisor = Math.Pow(10, mag * 3);

            double shortNumber = number / divisor;

            string suffix;
            switch (mag)
            {
                case 0:
                    suffix = string.Empty;
                    break;
                case 1:
                    suffix = "K";
                    break;
                case 2:
                    suffix = "M";
                    break;
                case 3:
                    suffix = "B";
                    break;
                default:
                    suffix = string.Empty;
                    break;
            }

            if (shortNumber == Math.Floor(shortNumber))
                return shortNumber.ToString("N0") + suffix;
            return shortNumber.ToString("N1") + suffix;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

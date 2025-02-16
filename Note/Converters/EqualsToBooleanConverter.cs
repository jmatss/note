using System.Globalization;
using System.Windows.Data;

namespace Note.Converters
{
    public class EqualsToBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool incrementFirstValue = parameter != null;
            if (incrementFirstValue && values[0] is int i && i != -1 && int.TryParse(parameter?.ToString(), out int incrementAmount))
            {
                values[0] = i + incrementAmount;
            }
            return object.Equals(values[0], values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

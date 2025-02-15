using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Collections;

namespace Note.Converters
{
    public class EnumerableToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IEnumerable enumerable)
            {
                throw new InvalidOperationException("Unknown type in EnumerableToVisibilityConverter: " + value?.GetType());
            }

            return enumerable.GetEnumerator().MoveNext() ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

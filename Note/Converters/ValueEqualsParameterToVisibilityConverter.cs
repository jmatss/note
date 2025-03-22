using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Note.Converters
{
    /// <summary>
    /// If the input value is equal to parameter, returns Visible.
    /// If the input value is NOT equal to parameter, returns Collapsed.
    /// </summary>
    public class ValueEqualsParameterToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Equals(value, parameter) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

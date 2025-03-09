using System.Globalization;
using System.Windows.Data;

namespace Note.Converters
{
    /// <summary>
    /// Converts an index of a TabGroup inside a grid into an index where
    /// GridSplitters have been added into the grid.
    /// </summary>
    public class IndexToGridSplitterIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int i)
            {
                throw new InvalidOperationException("Value not of type int: " + value?.GetType());
            }

            return 2 * i;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

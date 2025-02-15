using System.Globalization;
using System.Windows.Data;

namespace Note.Converters
{
    public class UriToFilenameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }
            if (value is not Uri uri)
            {
                throw new InvalidOperationException("UriToFilenameConverter not Uri type: " + value.GetType());
            }

            return uri.Segments[^1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

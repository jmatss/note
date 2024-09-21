using Editor.Range;
using Note.Rope;
using System.Globalization;
using System.Windows.Data;

namespace Editor.Converters
{
    public class SelectionsToLineInformationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
            {
                throw new ArgumentException("values.Length != 2");
            }

            var selections = (List<SelectionRange>)values[0];
            var rope = (Rope)values[1];

            var selection = selections.FirstOrDefault();
            if (selection == null)
            {
                return string.Empty;
            }

            string selectionText = string.Empty;
            if (selection.Length > 0)
            {
                selectionText = ", sl: " + selection.Length;
            }

            int lineIdx = rope.GetLineIndexForCharAtIndex(selection.InsertionPositionIndex);
            int firstCharIdx = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);

            int line = lineIdx + 1;
            int column = selection.InsertionPositionIndex - firstCharIdx + 1;
            return "ln: " + line + ", ch: " +  column + ", ct: " + (selection.InsertionPositionIndex + 1) + selectionText;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

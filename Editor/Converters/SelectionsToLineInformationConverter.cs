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
            if (values.Length != 3)
            {
                throw new ArgumentException("values.Length != 3");
            }

            var selections = (List<SelectionRange>)values[0];
            var highlights = (List<SelectionRange>)values[1];
            var rope = (Rope)values[2];

            var selection = selections.FirstOrDefault();
            if (selection == null)
            {
                return string.Empty;
            }

            string selectionText = string.Empty;
            if (selection.Length > 0)
            {
                selectionText = "(len: " + selection.Length + ", count: " + highlights.Count + ") ";
            }

            int lineIdx = rope.GetLineIndexForCharAtIndex(selection.InsertionPositionIndex);
            int firstCharIdx = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);

            int row = lineIdx + 1;
            int column = selection.InsertionPositionIndex - firstCharIdx + 1;
            return selectionText + "row: " + row + ", col: " +  column + ", char: " + (selection.InsertionPositionIndex + 1);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

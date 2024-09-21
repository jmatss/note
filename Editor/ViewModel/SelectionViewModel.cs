using Editor.Range;
using Note.Rope;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Editor.ViewModel
{
    public class SelectionViewModel
    {
        public SelectionViewModel(double x, double y, double width, double height, Brush brush)
        {
            this.X = x;
            this.Y = y;
            this.width = width;
            this.height = height;
            this.Brush = brush;
        }

        public double X { get; }

        public double Y { get; }

        private readonly double width;
        public double Width => double.Round(this.width);

        private readonly double height;
        public double Height => double.Round(this.height);

        public Brush Brush { get; }

        public static IEnumerable<SelectionViewModel> CalculateSelections(
            IEnumerable<LineViewModel> lines,
            IEnumerable<SelectionRange> selectionRanges,
            Brush brush
        )
        {
            List<SelectionViewModel> selections = new List<SelectionViewModel>();

            foreach (SelectionRange selectionRange in selectionRanges)
            {
                if (selectionRange.Length == 0)
                {
                    continue;
                }

                int selectionStartIdx = selectionRange.Start;
                int selectionEndIdx = selectionRange.End;

                foreach (LineViewModel line in lines)
                {
                    int lineStartIdx = line.StartCharIdx;
                    int lineEndIdx = line.EndCharIdx;

                    if (lineStartIdx == -1 || lineEndIdx == -1)
                    {
                        // TODO: How to handle? Is empty row, probably last row
                        continue;
                    }

                    if (selectionStartIdx > lineEndIdx || selectionEndIdx <= lineStartIdx)
                    {
                        // The selection isn't at this line
                        continue;
                    }

                    int startIdx = Math.Max(selectionStartIdx, lineStartIdx);
                    int endIdx = Math.Min(selectionEndIdx - 1, lineEndIdx);

                    var startChar = line.First(c => c.CharIdx == startIdx);
                    var endChar = line.First(c => c.CharIdx == endIdx);

                    double x = startChar.X;
                    double y = startChar.Y;
                    double width = endChar.X + endChar.Width - startChar.X;
                    double height = line.Height;

                    selections.Add(new SelectionViewModel(x, y, width, height, brush));
                }
            }

            return selections;
        }

        public static IEnumerable<SelectionRange> CalculateHighlightsInView(Rope rope, IEnumerable<LineViewModel> lines, string textToFind)
        {
            List<SelectionRange> highlights = new List<SelectionRange>();

            foreach (LineViewModel line in lines)
            {
                highlights.AddRange(FindHighlights(rope, textToFind, line.StartCharIdx, line.EndCharIdx));
            }

            return highlights;
        }

        public static IEnumerable<SelectionRange> CalculateHighlights(Rope rope, string textToFind)
        {
            return FindHighlights(rope, textToFind, 0, rope.GetTotalCharCount() - 1);
        }

        private static IEnumerable<SelectionRange> FindHighlights(Rope rope, string textToFind, int startCharIdx, int endCharIdx)
        {
            var highlights = new List<SelectionRange>();

            int i = 0;
            int charIdx = startCharIdx;
            int stringToFindLength = textToFind.Length;

            foreach ((char curCh, _) in rope.IterateChars(startCharIdx, endCharIdx - startCharIdx + 1))
            {
                if (curCh == textToFind[i])
                {
                    i++;

                    if (i >= stringToFindLength)
                    {
                        i = 0;
                        highlights.Add(new SelectionRange(charIdx + 1 - stringToFindLength, charIdx + 1, InsertionPosition.None));
                    }
                }
                else
                {
                    i = 0;
                }

                charIdx++;
            }

            return highlights;
        }
    }
}

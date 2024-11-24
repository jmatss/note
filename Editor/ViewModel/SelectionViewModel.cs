using Editor.Range;
using System.Windows.Media;
using Text;

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
            IEnumerable<RangeBase> ranges,
            Brush brush
        )
        {
            List<SelectionViewModel> selections = new List<SelectionViewModel>();

            foreach (RangeBase range in ranges)
            {
                if (range.Length == 0)
                {
                    continue;
                }

                int selectionStartIdx = range.Start;
                int selectionEndIdx = range.End;

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
                highlights.AddRange(
                    rope.FindAll(textToFind, line.StartCharIdx, line.EndCharIdx)
                        .Select(x => new SelectionRange(x.Item1, x.Item2, InsertionPosition.None))
                );
            }

            return highlights;
        }

        public static IEnumerable<SelectionRange> CalculateHighlights(Rope rope, string textToFind)
        {
            return rope.FindAll(textToFind, 0, rope.GetTotalCharCount())
                .Select(x => new SelectionRange(x.Item1, x.Item2, InsertionPosition.None));
        }
    }
}

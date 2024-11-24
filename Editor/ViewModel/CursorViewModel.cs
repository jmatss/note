using Editor.Range;
using System.Windows.Media;
using Text;

namespace Editor.ViewModel
{
    public class CursorViewModel
    {
        public CursorViewModel(double x, double y, double width, double height, Settings settings)
        {
            this.X = x;
            this.Y = y;
            this.width = width;
            this.height = height;
            this.Brush = settings.SelectionCursorColor;
        }

        public double X { get; }

        public double Y { get; }

        private readonly double width;
        public double Width => double.Round(this.width);

        private readonly double height;
        public double Height => double.Round(this.height);

        public Brush Brush { get; }

        public static IEnumerable<CursorViewModel> CalculateCursors(
            Rope rope,
            IEnumerable<LineViewModel> lines,
            IEnumerable<SelectionRange> selectionRanges,
            Settings settings
        )
        {
            List<CursorViewModel> cursors = new List<CursorViewModel>();

            int lastCharIdx = rope.GetTotalCharCount();
            if (lastCharIdx == 0 || lines.Count() == 0)
            {
                // No text, so no need to draw cursor
                return Enumerable.Empty<CursorViewModel>();
            }

            foreach (SelectionRange selectionRange in selectionRanges)
            {
                int cursorIdx = selectionRange.InsertionPositionIndex;

                if (cursorIdx == lastCharIdx)
                {
                    cursors.Add(CalculateLastCharCursor(lines.Last(), settings));
                }
                else
                {
                    CursorViewModel cursor = CalculateCursor(lines, settings, cursorIdx);
                    if (cursor != null)
                    {
                        cursors.Add(cursor);
                    }
                }
            }

            return cursors;
        }

        private static CursorViewModel CalculateLastCharCursor(LineViewModel line, Settings settings)
        {
            double x;
            double y;
            double width = 1;
            double height = line.Height;

            char? lastChar = line.LastOrDefault()?.FirstChar;
            if (lastChar != null && lastChar == LineViewModel.LINE_BREAK)
            {
                x = 0;
                y = line.Y + line.Height;
            }
            else
            {
                x = line.X + line.Width;
                y = line.Y;
            }

            return new CursorViewModel(x, y, width, height, settings);
        }

        private static CursorViewModel CalculateCursor(IEnumerable<LineViewModel> lines, Settings settings, int cursorIdx)
        {
            foreach (LineViewModel line in lines)
            {
                int lineStartIdx = line.StartCharIdx;
                int lineEndIdx = line.EndCharIdx;

                if (cursorIdx > lineEndIdx || cursorIdx < lineStartIdx)
                {
                    continue;
                }

                CharacterViewModel? cursorChar = line.FirstOrDefault(c => c.CharIdx == cursorIdx);

                double x = cursorChar?.X ?? line.X;
                double y = cursorChar?.Y ?? line.Y;
                double width = 1;
                double height = line.Height;

                return new CursorViewModel(x, y, width, height, settings);
            }

            return null;
        }
    }
}

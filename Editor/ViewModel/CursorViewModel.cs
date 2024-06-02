using System.Windows;
using System.ComponentModel;
using System.Windows.Shapes;
using Note.Rope;
using Editor.Range;

namespace Editor.ViewModel
{
    public class CursorViewModel : IDrawable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CursorViewModel(double x, double y, double width, double height, Settings settings)
        {
            this.X = x;
            this.Y = y;
            this.width = width;
            this.height = height;

            this.Draw(settings);
        }

        public double X { get; }

        public double Y { get; }

        private double width;
        public double Width => double.Round(this.width);

        private double height;
        public double Height => double.Round(this.height);

        public Thickness PositionCursor => new Thickness(double.Round(this.X), double.Round(this.Y), 0, 0);

        public Thickness PositionLineBackground => new Thickness(0, double.Round(this.Y), 0, 0);

        private UIElement element;
        public UIElement Element
        {
            get => this.element;
            set
            {
                this.element = value;
                this.OnPropertyChanged(nameof(this.Element));
            }
        }

        public void Draw(Settings settings)
        {
            Rectangle rectangle = new Rectangle()
            {
                Width = this.Width,
                Height = this.Height,
                Fill = settings.SelectionCursorColor,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            this.Element = rectangle;
        }

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

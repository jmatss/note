using System.Windows;
using System.ComponentModel;
using System.Windows.Shapes;
using Editor.Range;

namespace Editor.ViewModel
{
    public class SelectionViewModel : IDrawable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SelectionViewModel(double x, double y, double width, double height, Settings settings)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;

            this.Draw(settings);
        }

        public double X { get; }

        public double Y { get; }

        public double Width { get; }

        public double Height { get; }

        public Thickness Position => new Thickness(this.X, this.Y, 0, 0);

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
                Fill = settings.SelectionBackgroundColor,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            this.Element = rectangle;
        }

        public static IEnumerable<SelectionViewModel> CalculateSelections(
            IEnumerable<LineViewModel> lines,
            IEnumerable<SelectionRange> selectionRanges,
            Settings settings
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

                    selections.Add(new SelectionViewModel(x, y, width, height, settings));
                }
            }

            return selections;
        }
    }
}

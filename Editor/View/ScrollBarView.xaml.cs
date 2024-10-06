using Editor.Range;
using Editor.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Editor.View
{
    public partial class ScrollBarView : UserControl
    {
        private readonly DrawingGroup drawingGroup = new DrawingGroup();

        public ScrollBarView()
        {
            InitializeComponent();
        }

        public FileViewModel ViewModel => (FileViewModel)base.DataContext;

        public static double ArrowWidth => 20;

        public static double ArrowHeight => 20;

        public Point? MouseCapturePosition { get; set; }

        public Rect ScrollBarRect { get; set; } = Rect.Empty;

        public double CharDrawHeight { get; set; } = 0;

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is FileViewModel vm)
            {
                vm.OnDraw += this.Draw;
                vm.OnDrawSelections += this.Draw;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawDrawing(drawingGroup);
        }

        public void Draw(double _charDrawWidth, double charDrawHeight)
        {
            this.CharDrawHeight = charDrawHeight;
            using (DrawingContext drawingContext = this.drawingGroup.Open())
            {
                this.DrawBackground(drawingContext);
                this.DrawHighlights(
                    drawingContext,
                    charDrawHeight,
                    this.ViewModel.Highlights,
                    new SolidColorBrush(Color.FromArgb(255, 49, 40, 20))
                );
                this.DrawHighlights(
                    drawingContext,
                    charDrawHeight,
                    this.ViewModel.Selections,
                    new SolidColorBrush(Color.FromArgb(255, 85, 85, 85))
                );
                this.DrawScrollBar(drawingContext, charDrawHeight);
            }
        }

        private void DrawBackground(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(
                LineNumbersView.BackgroundColor,
                null,
                new Rect(
                    new Point(0, 0),
                    new Size(
                        this.ActualWidth,
                        this.ActualHeight
                    )
                )
            );
        }

        private void DrawScrollBar(DrawingContext drawingContext, double charDrawHeight)
        {
            int startCharIdx = this.ViewModel.Lines.FirstOrDefault()?.StartCharIdx ?? -1;
            int endCharIdx = this.ViewModel.Lines.LastOrDefault()?.EndCharIdx ?? -1;

            if (startCharIdx == -1 || endCharIdx == -1)
            {
                return;
            }

            int possibleAmountOfLinesInView = (int)((this.ActualHeight) / charDrawHeight);
            int allLines = this.ViewModel.Rope.GetTotalLineBreaks() + possibleAmountOfLinesInView;
            allLines = allLines == 0 ? 1 : allLines;
            double oneLineHeight = (this.ActualHeight - ArrowHeight * 2) / allLines;
            int startLineIdx = this.ViewModel.Rope.GetLineIndexForCharAtIndex(startCharIdx);
            int lineSpan = possibleAmountOfLinesInView;

            (double scrollBarY, double scrollBarHeight) = CalculateYPositionAndHeight(
                allLines,
                oneLineHeight,
                startLineIdx,
                lineSpan
            );

            if (scrollBarHeight > 0)
            {
                this.ScrollBarRect = new Rect(
                    new Point(0, scrollBarY),
                    new Size(
                        this.ActualWidth,
                        scrollBarHeight
                    )
                );
            }
            else
            {
                this.ScrollBarRect = Rect.Empty;
            }

            drawingContext.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
                null,
                this.ScrollBarRect
            );
        }

        private void DrawHighlights(DrawingContext drawingContext, double charDrawHeight, IEnumerable<SelectionRange> highlights, Brush brush)
        {
            int startCharIdx = this.ViewModel.Lines.FirstOrDefault()?.StartCharIdx ?? -1;
            int endCharIdx = this.ViewModel.Lines.LastOrDefault()?.EndCharIdx ?? -1;

            if (startCharIdx == -1 || endCharIdx == -1)
            {
                return;
            }

            int possibleAmountOfLinesInView = (int)((this.ActualHeight) / charDrawHeight);
            int allLines = this.ViewModel.Rope.GetTotalLineBreaks() + possibleAmountOfLinesInView;
            allLines = allLines == 0 ? 1 : allLines;
            double oneLineHeight = (this.ActualHeight - ArrowHeight * 2) / allLines;

            foreach (SelectionRange highlight in highlights)
            {
                int startLineIdx = this.ViewModel.Rope.GetLineIndexForCharAtIndex(highlight.Start);
                int endLineIdx = this.ViewModel.Rope.GetLineIndexForCharAtIndex(highlight.End);
                int lineSpan = endLineIdx - startLineIdx + 1;

                (double highlightY, double highlightHeight) = CalculateYPositionAndHeight(
                    allLines,
                    oneLineHeight,
                    startLineIdx,
                    lineSpan
                );

                if (highlightHeight > 0)
                {
                    var highlightRect = new Rect(
                        new Point(0, highlightY),
                        new Size(
                            this.ActualWidth,
                            highlightHeight
                        )
                    );
                    drawingContext.DrawRectangle(
                        brush,
                        null,
                        highlightRect
                    );
                }
            }
        }

        /// <summary>
        /// Caluclates the y-position and the height of something that are to be drawn
        /// in the scrollbar. This can be the thumb of the scrollbar or some sort of highlighting.
        /// </summary>
        /// <param name="allLines">Amount of line breaks in rope + amount of lines that can be displayed in the view</param>
        /// <param name="oneLineHeight">The height that one line takes up in the scrollbar</param>
        /// <param name="startLineIdx">The index of the line that represents the top of the thing that we are to draw</param>
        /// <param name="lineSpan">How many actual lines this thing spans</param>
        /// <returns>The y-position and the height</returns>
        private (double, double) CalculateYPositionAndHeight(int allLines, double oneLineHeight, int startLineIdx, int lineSpan)
        {
            double y = ArrowHeight + oneLineHeight * startLineIdx;
            double height = (lineSpan * (this.ActualHeight - ArrowHeight * 2)) / (allLines);

            if (height > 0)
            {
                return (y, height);
            }
            else
            {
                return (0, 0);
            }
        }

        private void ScrollBarUpArrow_Click(object sender, RoutedEventArgs e)
        {
            int scrollIncrement = this.ViewModel.Settings.ScrollIncrement;
            this.ViewModel.HandleScroll(-scrollIncrement);
        }

        private void ScrollBarDownArrow_Click(object sender, RoutedEventArgs e)
        {
            int scrollIncrement = this.ViewModel.Settings.ScrollIncrement;
            this.ViewModel.HandleScroll(scrollIncrement);
        }

        private void ScrollBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point position = GetPosition(sender, e);
            if (this.ScrollBarRect.Contains(position))
            {
                if (Mouse.Capture(sender as IInputElement))
                {
                    this.MouseCapturePosition = position;
                }
            }
            else
            {
                int linesInView = (int)(this.ActualHeight / this.CharDrawHeight);

                if (position.Y < this.ScrollBarRect.Y)
                {
                    // Clicked above scrollbar
                    linesInView *= -1;
                }
                else
                {
                    // Clicked below scrollbar
                }

                this.ViewModel.HandleScroll(linesInView);
            }
        }

        private void ScrollBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.MouseCapturePosition = null;
            Mouse.Capture(null);
        }

        private void ScrollBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.MouseCapturePosition is Point prevPosition)
            {
                Point position = GetPosition(sender, e);
                bool scrollBarWasMoved = this.ViewModel.HandleScrollBarMouseLeftMove(position, prevPosition);
                if (scrollBarWasMoved)
                {
                    this.MouseCapturePosition = position;
                }
            }
        }

        public static Point GetPosition(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(sender as IInputElement);
            position.Y += ArrowHeight;
            return position;
        }
    }
}

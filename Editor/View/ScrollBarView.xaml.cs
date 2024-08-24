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

            double scrollBarY = ArrowHeight + oneLineHeight * startLineIdx;
            double scrollBarHeight = (possibleAmountOfLinesInView * (this.ActualHeight - ArrowHeight * 2)) / (allLines);

            this.ScrollBarRect = new Rect(
                new Point(0, scrollBarY),
                new Size(
                    this.ActualWidth,
                    scrollBarHeight
                )
            );

            drawingContext.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(255, 15, 15, 15)),
                null,
                this.ScrollBarRect
            );
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

using Editor.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Editor.View
{
    public partial class LineNumbersView : UserControl
    {
        private readonly DrawingGroup drawingGroup = new DrawingGroup();

        public static Brush BackgroundColor { get; set; } = new SolidColorBrush(Color.FromArgb(255, 25, 25, 25));

        public static TranslateTransform PaddingTransform = new TranslateTransform(LineNumbersSpacing.Left, LineNumbersSpacing.Top);

        public static Thickness LineNumbersSpacing => new Thickness(15, TextView.TextSpacing.Top, 15, TextView.TextSpacing.Bottom);

        public LineNumbersView()
        {
            InitializeComponent();
            BackgroundColor.Freeze();
        }

        public FileViewModel ViewModel => (FileViewModel)base.DataContext;

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is FileViewModel vm)
            {
                vm.OnDraw += this.Draw;
            }
        }

        private void LineNumbers_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point position = TextView.NormalizedPosition(sender, e, LineNumbersSpacing);
            this.ViewModel.HandleLineNumbersMouseLeftClick(position);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawDrawing(drawingGroup);
        }

        public void Draw()
        {
            using (DrawingContext drawingContext = this.drawingGroup.Open())
            {
                this.Width = (this.ViewModel.LineNumbers?.Width ?? 0) + LineNumbersSpacing.Left + LineNumbersSpacing.Right;
                this.Width = Math.Ceiling(this.Width);

                this.DrawBackground(drawingContext, LineNumbersSpacing);
                drawingContext.PushTransform(PaddingTransform);
                TextView.DrawGlyphRun(drawingContext, this.ViewModel.LineNumbers);
            }
        }

        private void DrawBackground(DrawingContext drawingContext, Thickness padding)
        {
            drawingContext.DrawRectangle(
                LineNumbersView.BackgroundColor,
                null,
                new Rect(
                    new Point(0, 0),
                    new Size(
                        this.Width,
                        this.ActualHeight
                    )
                )
            );
        }
    }
}

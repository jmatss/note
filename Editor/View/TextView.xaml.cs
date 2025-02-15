using Editor.ViewModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Editor.View
{
    public partial class TextView
    {
        private readonly DrawingGroup drawingGroupText = new DrawingGroup();
        private readonly DrawingGroup drawingGroupSelections = new DrawingGroup();

        public static Brush BackgroundColor { get; set; } = new SolidColorBrush(Color.FromArgb(255, 31, 31, 31));

        public static TranslateTransform PaddingTransform = new TranslateTransform(TextSpacing.Left, TextSpacing.Top);

        public TextView()
        {
            InitializeComponent();
            BackgroundColor.Freeze();
        }

        public FileViewModel ViewModel => (FileViewModel)base.DataContext;

        public bool MouseIsCaptured { get; set; } = false;

        public static Thickness TextSpacing => new Thickness(6, 2, 6, 2);

        /// <summary>
        /// Spacing used for the lines shown at the top and bottom of the currently
        /// selected line
        /// </summary>
        public static Thickness TextSpacingTopBottom => new Thickness(-6, 0, -6, 0);

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is FileViewModel vm)
            {
                vm.OnDraw += this.Draw;
                vm.OnDrawSelections += this.DrawSelections;
                this.ViewModel.ViewWidth = this.TextArea.ActualWidth;
                this.ViewModel.ViewHeight = this.TextArea.ActualHeight;
                this.ViewModel.Recalculate(true);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawDrawing(drawingGroupText);
            drawingContext.DrawDrawing(drawingGroupSelections);
        }

        public void Draw(double charDrawWidth, double charDrawHeight)
        {
            using (DrawingContext drawingContext = this.drawingGroupText.Open())
            {
                DrawBackground(drawingContext, this.ViewModel, TextSpacing);
                drawingContext.PushTransform(PaddingTransform);
                DrawGlyphRun(drawingContext, this.ViewModel.Text);
                DrawGlyphRun(drawingContext, this.ViewModel.CustomCharWhitespace);
                DrawCustomChars(drawingContext, this.ViewModel.CustomChars);
            }

            this.DrawSelections(charDrawWidth, charDrawHeight);
        }

        public void DrawSelections(double _charDrawWidth, double _charDrawHeight)
        {
            var errorBrush = (VisualBrush)this.FindResource("DiagnosticsBrushRed");

            using (DrawingContext drawingContext = this.drawingGroupSelections.Open())
            {
                drawingContext.PushTransform(PaddingTransform);
                DrawSelections(drawingContext, this.ViewModel.HighlightsInView);
                DrawSelections(drawingContext, this.ViewModel.SelectionsInView);
                DrawUnderlines(drawingContext, this.ViewModel.DiagnosticsInView, errorBrush);
                DrawCursors(drawingContext, this.ViewModel.Cursors);
            }
        }

        private static void DrawBackground(DrawingContext drawingContext, FileViewModel textVm, Thickness padding)
        {
            drawingContext.DrawRectangle(
                TextView.BackgroundColor,
                null,
                new Rect(
                    new Point(0, 0),
                    new Size(
                        textVm.ViewWidth + padding.Left + padding.Right,
                        textVm.ViewHeight + padding.Top + padding.Bottom
                    )
                )
            );
        }

        public static void DrawGlyphRun(DrawingContext drawingContext, GlyphRunViewModel? glyphRunVm)
        {
            if (glyphRunVm != null && glyphRunVm.GlyphRun != null)
            {
                if (glyphRunVm.Width != null)
                {
                    // TODO: Need ot implement logic for this.
                    // Used to change the width if LineNumbersView.
                    //this.View.Width = double.Round(this.ViewModel.Width.Value);
                }
                drawingContext.DrawGlyphRun(glyphRunVm.Brush, glyphRunVm.GlyphRun);
            }
        }

        private static void DrawCustomChars(DrawingContext drawingContext, CustomCharViewModel? customCharVm)
        {
            if (customCharVm == null)
            {
                return;
            }

            foreach (CharacterViewModel ch in customCharVm)
            {
                drawingContext.DrawRoundedRectangle(
                    customCharVm.BackgroundBrush,
                    null,
                    new Rect(ch.X, ch.Y, ch.Width, ch.Height),
                    1,
                    1
                );
            }

            if (customCharVm.GlyphRun != null)
            {
                drawingContext.PushTransform(new ScaleTransform(CustomCharViewModel.DownScaleX, CustomCharViewModel.DownScaleY));
                drawingContext.DrawGlyphRun(customCharVm.ForegroundBrush, customCharVm.GlyphRun);
                drawingContext.Pop();
            }
        }

        private static void DrawSelections(DrawingContext drawingContext, IEnumerable<SelectionViewModel> selectionVms)
        {
            foreach (SelectionViewModel selectionVm in selectionVms)
            {
                drawingContext.DrawRectangle(
                    selectionVm.Brush,
                    null,
                    new Rect(
                        new Point(selectionVm.X, selectionVm.Y),
                        new Size(selectionVm.Width, selectionVm.Height)
                    )
                );
            }
        }

        private static void DrawUnderlines(DrawingContext drawingContext, IEnumerable<SelectionViewModel> selectionVms, VisualBrush brush)
        {
            double extraPaddingUnderText = 1;
            foreach (SelectionViewModel selectionVm in selectionVms)
            {
                // https://stackoverflow.com/a/7072004
                TranslateTransform transform = new TranslateTransform(Math.Round(selectionVm.X), Math.Round(selectionVm.Y + selectionVm.Height - brush.Viewport.Height + extraPaddingUnderText));
                transform.Freeze();
                drawingContext.PushTransform(transform);
                drawingContext.DrawRectangle(
                    brush,
                    null,
                    new Rect(0, 0, Math.Round(selectionVm.Width), brush.Viewport.Height)
                );
                drawingContext.Pop();
            }
        }

        private static void DrawCursors(DrawingContext drawingContext, IEnumerable<CursorViewModel> cursorVms)
        {
            foreach (CursorViewModel cursorVm in cursorVms)
            {
                drawingContext.DrawRectangle(
                    cursorVm.Brush,
                    null,
                    new Rect(
                        new Point(cursorVm.X, cursorVm.Y),
                        new Size(cursorVm.Width, cursorVm.Height)
                    )
                );
            }
        }

        private void Text_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point position = GetPosition(sender, e);
            if (e.ClickCount == 2)
            {
                this.ViewModel.HandleTextMouseLeftDoubleClick(position);
            }
            else
            {
                this.ViewModel.HandleTextMouseLeftClick(position);
            }
            this.MouseIsCaptured = Mouse.Capture(sender as IInputElement);
        }

        private void Text_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.MouseIsCaptured = false;
            Mouse.Capture(null);
        }

        private void Text_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.MouseIsCaptured)
            {
                Point position = GetPosition(sender, e);
                this.ViewModel.HandleTextMouseLeftMove(position);
            }
        }

        public static Point GetPosition(object sender, MouseEventArgs e)
        {
            return e.GetPosition(sender as IInputElement);
        }

        private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ViewModel == null)
            {
                return;
            }

            bool isNewSize = this.ViewModel.ViewWidth != e.NewSize.Width || this.ViewModel.ViewHeight != e.NewSize.Height;
            if (isNewSize)
            {
                this.ViewModel.ViewWidth = e.NewSize.Width;
                this.ViewModel.ViewHeight = e.NewSize.Height;
                this.ViewModel.Recalculate(false);
            }
        }
    }
}

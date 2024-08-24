using Editor.ViewModel;
using System.Diagnostics;
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
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawDrawing(drawingGroupText);
            drawingContext.DrawDrawing(drawingGroupSelections);
        }

        public void Draw(double _charDrawWidth, double _charDrawHeight)
        {
            using (DrawingContext drawingContext = this.drawingGroupText.Open())
            {
                DrawBackground(drawingContext, this.ViewModel, TextSpacing);
                drawingContext.PushTransform(PaddingTransform);
                DrawGlyphRun(drawingContext, this.ViewModel.Text);
                DrawGlyphRun(drawingContext, this.ViewModel.CustomCharWhitespace);
                DrawCustomChars(drawingContext, this.ViewModel.CustomChars);
            }

            this.DrawSelections();
        }

        public void DrawSelections()
        {
            using (DrawingContext drawingContext = this.drawingGroupSelections.Open())
            {
                drawingContext.PushTransform(PaddingTransform);
                DrawSelections(drawingContext, this.ViewModel.Selections);
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

        private void Text_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int scrollIncrement = this.ViewModel.Settings.ScrollIncrement;
            if (e.Delta > 0)
            {
                this.ViewModel.HandleScroll(-scrollIncrement);
            }
            else if (e.Delta < 0)
            {
                this.ViewModel.HandleScroll(scrollIncrement);
            }
        }
    }
}

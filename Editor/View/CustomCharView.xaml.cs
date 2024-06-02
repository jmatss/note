using Editor.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Editor.View
{
    /// <summary>
    /// Interaction logic for CustomCharWhitespaceView.xaml
    /// </summary>
    public partial class CustomCharView : UserControl
    {
        private readonly DrawingGroup drawingGroup = new DrawingGroup();

        public CustomCharView()
        {
            InitializeComponent();
        }

        public CustomCharViewModel ViewModel => base.DataContext as CustomCharViewModel;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawDrawing(this.drawingGroup);
        }

        // Since changing the ViewModel doesn't automatically redraw/recreate the view
        // (if the new ViewModel has the same type as the old one), we do this hack to
        // redraw the view when the ViewModel is changed.
        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            using (DrawingContext drawingContext = this.drawingGroup.Open())
            {
                foreach (CharacterViewModel ch in this.ViewModel)
                {
                    double radius = ch.Width / 4;
                    drawingContext.DrawRoundedRectangle(
                        this.ViewModel.BackgroundBrush,
                        null,
                        new Rect(ch.X, ch.Y, ch.Width, ch.Height),
                        1,
                        1
                    );
                }

                if (this.ViewModel?.GlyphRun != null)
                {
                    drawingContext.PushTransform(new ScaleTransform(CustomCharViewModel.DownScaleX, CustomCharViewModel.DownScaleY));
                    drawingContext.DrawGlyphRun(this.ViewModel.ForegroundBrush, this.ViewModel.GlyphRun);
                    drawingContext.Pop();
                }
            }
        }
    }
}

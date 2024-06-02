
using Editor.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Editor.View
{
    public partial class GlyphRunView : UserControl
    {
        private readonly DrawingGroup drawingGroup = new DrawingGroup();

        public GlyphRunView()
        {
            InitializeComponent();
        }

        public GlyphRunViewModel ViewModel => base.DataContext as GlyphRunViewModel;

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
                if (this.ViewModel?.GlyphRun != null)
                {
                    if (this.ViewModel.Width != null)
                    {
                        this.View.Width = double.Round(this.ViewModel.Width.Value);
                    }
                    drawingContext.DrawGlyphRun(this.ViewModel.Brush, this.ViewModel.GlyphRun);
                }
            }
        }
    }
}

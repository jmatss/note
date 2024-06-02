using Editor.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Editor.View
{
    /// <summary>
    /// Interaction logic for CustomCharWhitespaceView.xaml
    /// </summary>
    public partial class CustomCharWhitespaceView : UserControl
    {
        private readonly DrawingGroup drawingGroup = new DrawingGroup();

        public CustomCharWhitespaceView()
        {
            InitializeComponent();
        }

        public CustomCharWhitespaceViewModel ViewModel => base.DataContext as CustomCharWhitespaceViewModel;

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
                    drawingContext.DrawEllipse(
                        this.ViewModel.Brush,
                        null,
                        new Point(ch.CenterX, ch.CenterY),
                        radius,
                        radius
                    );
                    
                }
            }
        }
    }
}

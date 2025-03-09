using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Note.Tabs.TabGroupContainerViewModel;

namespace Note.Tabs
{
    public partial class TabGroupView : UserControl
    {
        public TabGroupView()
        {
            InitializeComponent();
        }

        public TabGroupViewModel ViewModel => (TabGroupViewModel)base.DataContext;

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is TabGroupViewModel vm)
            {
                vm.MousePositionDragPosition = this.MousePositionDragPosition;
                vm.MousePositionHeaderIndex = this.MousePositionHeaderIndex;
            }
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.ViewModel.FocusedFileChanged();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton is MouseButton.Left)
            {
                int index = (int)((FrameworkElement)sender).Tag;
                bool isMouseCaptured = Mouse.Capture(sender as IInputElement);
                this.ViewModel.MouseDown(index, isMouseCaptured);
                e.Handled = true;
            }
        }

        private void Header_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton is MouseButton.Left)
            {
                Mouse.Capture(null);
                this.ViewModel.MouseUp();
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                this.XButton_MouseUp(sender, e);
            }
        }

        private void Header_MouseMove(object sender, MouseEventArgs e)
        {
            this.ViewModel.MouseDrag();
        }

        private void XButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton is MouseButton.Left or MouseButton.Middle)
            {
                e.Handled = true;
                int index = (int)((FrameworkElement)sender).Tag;
                bool wasWholeTabGroupRemoved = this.ViewModel.RemoveTab(index);
                if (!wasWholeTabGroupRemoved)
                {
                    // TODO: Don't call this here? Handle from `TabsContainer` instead.
                    this.ViewModel.FocusedFileChanged();
                }
            }
        }

        private void XButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private Position? MousePositionDragPosition()
        {
            Point mousePosition = Mouse.GetPosition(this.ContentGrid);

            var result = VisualTreeHelper.HitTest(this.ContentGrid, mousePosition);
            if (result == null)
            {
                return null;
            }

            // Split the `ContentGrid` rectangle into triangles and se
            // in which direction we should split the tab group when
            // we drop the tab.
            //
            // Example of the triangles below:
            //  ________
            // |\      /|
            // | \ T  / |
            // |  \  /  |
            // |   \/   |
            // | L /\ R |
            // |  /  \  |
            // | /  B \ |
            // |/______\|
            var topLeft = new Point(0, 0);
            var topRight = new Point(this.ContentGrid.ActualWidth, 0);
            var bottomLeft = new Point(0, this.ContentGrid.ActualHeight);
            var bottomRight = new Point(this.ContentGrid.ActualWidth, this.ContentGrid.ActualHeight);
            var center = new Point(this.ContentGrid.ActualWidth / 2, this.ContentGrid.ActualHeight / 2);

            if (TriangleContains(mousePosition, topLeft, topRight, center))
            {
                return Position.Top;
            }
            else if (TriangleContains(mousePosition, bottomLeft, bottomRight, center))
            {
                return Position.Bottom;
            }
            else if (TriangleContains(mousePosition, bottomLeft, topLeft, center))
            {
                return Position.Left;
            }
            else if (TriangleContains(mousePosition, topRight, bottomRight, center))
            {
                return Position.Right;
            }
            else
            {
                throw new UnreachableException("Unknown drag position");
            }
        }

        private int MousePositionHeaderIndex()
        {
            Point mousePosition = Mouse.GetPosition(this.HeadersControl);
            List<Rect> headers = this.HeaderRects();

            double leftX = headers.First().X;
            double topY = headers.First().Y;
            double bottomY = headers.Last().Y + headers.Last().Height;

            int index = -1;

            for (int i = 0; i < headers.Count; i++)
            {
                Rect curHeader = headers[i];
                Rect? nextHeader = i < headers.Count - 1 ? headers[i + 1] : null;

                // If isInFirstColum: I cover everything left of me
                bool isInFirstColum = curHeader.X <= leftX;
                // If isInLastColumn: I cover everything right of me
                bool isInLastColumn = nextHeader == null || curHeader.X >= nextHeader.Value.X;
                // If isInTopRow: I cover everything above me
                bool isInTopRow = curHeader.Y <= topY;
                // If isInBottomRow: I cover everything below me
                bool isInBottomRow = curHeader.Y + curHeader.Height >= bottomY;

                double x1 = 0;
                double y1 = 0;
                double x2 = curHeader.X + curHeader.Width / 2;
                double y2 = 0;

                if (isInFirstColum || i == 0)
                {
                    x1 = double.MinValue;
                }
                else
                {
                    Rect prevHeader = headers[i - 1];
                    x1 = prevHeader.X + prevHeader.Width / 2;
                }

                if (isInTopRow)
                {
                    y1 = double.MinValue;
                }
                else
                {
                    y1 = curHeader.Y;
                }

                if (isInBottomRow)
                {
                    y2 = double.MaxValue;
                }
                else
                {
                    y2 = RowBelowY(headers, curHeader.Y, i);
                }

                if (Contains(new Point(x1, y1), new Point(x2, y2), mousePosition))
                {
                    index = i;
                    break;
                }
                else if (isInLastColumn && Contains(new Point(x2, y1), new Point(double.MaxValue, y2), mousePosition))
                {
                    index = i + 1;
                    break;
                }
            }

            if (index == -1)
            {
                throw new UnreachableException("Unable to find header mouse move index");
            }

            return index;
        }

        private static double RowBelowY(List<Rect> headers, double y, int index)
        {
            for (int i = index + 1; i < headers.Count; i++)
            {
                var header = headers[i];
                if (y != header.Y)
                {
                    double rowBelowY = header.Y;
                    return rowBelowY;
                }
            }

            throw new UnreachableException("isInBottomRow false, but unable to find row below");
        }

        private List<Rect> HeaderRects()
        {
            var rects = new List<Rect>();

            foreach (var fileViewModel in this.ViewModel.Tabs)
            {
                var headerView = (UIElement)this.HeadersControl.ItemContainerGenerator.ContainerFromItem(fileViewModel);
                Point position = headerView.TranslatePoint(new Point(), this.HeadersControl);
                Size size = headerView.RenderSize;
                rects.Add(new Rect(position, size));
            }

            return rects;
        }

        /// <summary>
        /// Checks if the point `position` is contained inside the rectangle represented
        /// by `p1` (upper-left corner) & `p2` (bottom-right corner)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool Contains(Point p1, Point p2, Point position)
        {
            return position.X >= p1.X && position.X <= p2.X &&
                   position.Y >= p1.Y && position.Y <= p2.Y;
        }

        /// <summary>
        /// Checks if the triangle created with the three `v` parameters contains
        /// the point `pt`.
        /// 
        /// Adapted from: https://stackoverflow.com/a/2049593
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <returns></returns>
        private bool TriangleContains(Point pt, Point v1, Point v2, Point v3)
        {
            double d1 = Sign(pt, v1, v2);
            double d2 = Sign(pt, v2, v3);
            double d3 = Sign(pt, v3, v1);
            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);
        }

        private double Sign(Point p1, Point p2, Point p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Note
{
    public partial class TabsView : UserControl
    {
        public TabsView()
        {
            InitializeComponent();
        }

        public TabsViewModel ViewModel => (TabsViewModel)base.DataContext;

        public bool MouseIsCaptured { get; set; } = false;

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.ViewModel.GotFocus();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton is MouseButton.Left)
            {
                int index = (int)((FrameworkElement)sender).Tag;
                this.ViewModel.SelectedIndex = index;
                this.ViewModel.GotFocus();

                this.MouseIsCaptured = Mouse.Capture(sender as IInputElement);
                this.ViewModel.OriginalCapturedMouseTabIndex = index;
                e.Handled = true;
            }
        }

        private void Header_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton is MouseButton.Left)
            {
                int capturedMouseTabIndex = this.ViewModel.CapturedMouseTabIndex;
                int originalCaptureMouseTabIndex = this.ViewModel.OriginalCapturedMouseTabIndex;

                this.MouseIsCaptured = false;
                this.ViewModel.CapturedMouseTabIndex = -1;
                this.ViewModel.OriginalCapturedMouseTabIndex = -1;
                Mouse.Capture(null);

                if (capturedMouseTabIndex != -1)
                {
                    // The `capturedMouseTabIndex` represents the indices between the tab geadings,
                    // so decrement the index to get an index that represents a specific tab heading.
                    if (capturedMouseTabIndex > originalCaptureMouseTabIndex)
                    {
                        capturedMouseTabIndex--;
                    }

                    if (capturedMouseTabIndex != originalCaptureMouseTabIndex)
                    {
                        this.ViewModel.MoveTab(originalCaptureMouseTabIndex, capturedMouseTabIndex);
                    }
                }
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                this.XButton_MouseUp(sender, e);
            }
        }

        private void Header_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.MouseIsCaptured)
            {
                List<Rect> headers = HeaderRects();
                Point position = e.GetPosition(this.HeadersControl);
                this.ViewModel.HandleHeaderMouseMove(headers, position);
            }
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

        private void XButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton is MouseButton.Left or MouseButton.Middle)
            {
                int index = (int)((FrameworkElement)sender).Tag;
                this.ViewModel.Remove(index);
                e.Handled = true;
            }
        }

        private void XButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}

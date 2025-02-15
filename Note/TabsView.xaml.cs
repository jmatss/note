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

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.ViewModel.GotFocus();
        }

        private void Header_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton is MouseButton.Left or MouseButton.Right)
            {
                int index = (int)((FrameworkElement)sender).Tag;
                this.ViewModel.SelectedIndex = index;
                this.ViewModel.GotFocus();
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                this.XButton_MouseUp(sender, e);
            }
        }

        private void XButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton is MouseButton.Left or MouseButton.Right or MouseButton.Middle)
            {
                int index = (int)((FrameworkElement)sender).Tag;
                this.ViewModel.Remove(index);
                e.Handled = true;
            }
        }
    }
}

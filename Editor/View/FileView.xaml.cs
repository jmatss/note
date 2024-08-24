using Editor.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace Editor.View
{
    public partial class FileView : UserControl
    {
        public FileView()
        {
            InitializeComponent();
        }

        public FileViewModel ViewModel => (FileViewModel)base.DataContext;

        private void File_MouseWheel(object sender, MouseWheelEventArgs e)
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

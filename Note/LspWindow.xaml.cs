using System.Windows;

namespace Note
{
    public partial class LspWindow : Window
    {
        public LspWindow(LspWindowViewModel viewModel)
        {
            base.DataContext = viewModel;
            InitializeComponent();
        }

        public LspWindowViewModel ViewModel => (LspWindowViewModel)base.DataContext;

        private void ButtonLaunch_Click(object sender, RoutedEventArgs e)
        {
            bool isLaunched = this.ViewModel.Launch();
            if (isLaunched)
            {
                this.Close();
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

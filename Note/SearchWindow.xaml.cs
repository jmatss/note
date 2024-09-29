using System.Windows;

namespace Note
{
    public partial class SearchWindow : Window
    {
        public SearchWindow(SearchWindowViewModel viewModel)
        {
            base.DataContext = viewModel;
            InitializeComponent();
        }

        public SearchWindowViewModel ViewModel => (SearchWindowViewModel)base.DataContext;

        private void ButtonFind_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.FindOperation();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

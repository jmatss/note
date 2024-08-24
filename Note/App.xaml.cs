using System.Windows;

namespace Note
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var viewModel = new MainWindowViewModel();
            var view = new MainWindowView(viewModel);
            view.Show();
        }
    }
}

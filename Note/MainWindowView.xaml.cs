using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static Editor.HandleInput;

namespace Note
{
    public partial class MainWindowView : Window
    {
        public MainWindowView(MainWindowViewModel viewModel)
        {
            base.DataContext = viewModel;
            InitializeComponent();
        }

        public MainWindowViewModel ViewModel => (MainWindowViewModel)base.DataContext;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key;
            Modifiers modifiers = new Modifiers(e.KeyboardDevice);
            Trace.WriteLine("Window_KeyDown - key: " + key + ", modifiers: " + modifiers);
            e.Handled = this.ViewModel.KeyInput(key, modifiers);
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            string text = e.Text;
            Trace.WriteLine("Window_TextInput - text: " + text);
            e.Handled = this.ViewModel.TextInput(text);
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.OpenFile();
        }
    }
}
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

            switch (key)
            {
                case Key.A when modifiers.Ctrl:
                case Key.Left:
                case Key.Up:
                case Key.Right:
                case Key.Down:
                case Key.PageUp:
                case Key.Next:
                case Key.End:
                case Key.Home:
                case Key.Back:
                case Key.Delete:
                case Key.Enter:
                case Key.LineFeed:
                case Key.Tab:
                    this.ViewModel.TextViewModel?.HandleSpecialKeys(key, modifiers);
                    break;

                case Key.C when modifiers.Ctrl:
                    string? copyText = this.ViewModel.TextViewModel?.Read();
                    if (!string.IsNullOrEmpty(copyText))
                    {
                        Clipboard.SetText(copyText);
                    }
                    break;

                case Key.V when modifiers.Ctrl:
                    string pasteText = Clipboard.GetText();
                    this.ViewModel.TextViewModel?.Write(pasteText);
                    break;

                case Key.X when modifiers.Ctrl:
                    string? cutText = this.ViewModel.TextViewModel?.Read();
                    if (!string.IsNullOrEmpty(cutText))
                    {
                        Clipboard.SetText(cutText);
                        this.ViewModel.TextViewModel?.Write(string.Empty);
                    }
                    break;

                default:
                    break;
            }
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            string text = e.Text;

            Trace.WriteLine("Window_TextInput - text: " + text);

            if (!string.IsNullOrEmpty(text) && !char.IsControl(text.First()))
            {
                this.ViewModel.TextViewModel?.HandlePrintableKeys(e.Text);
            }
        }
    }
}
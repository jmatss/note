using Editor.Range;
using Microsoft.Win32;
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

            bool isHandled = true;

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
                    this.ViewModel.FileViewModel?.HandleSpecialKeys(key, modifiers);
                    break;

                case Key.C when modifiers.Ctrl:
                    string? copyText = this.ViewModel.FileViewModel?.Read();
                    if (!string.IsNullOrEmpty(copyText))
                    {
                        Clipboard.SetText(copyText);
                    }
                    break;

                case Key.V when modifiers.Ctrl:
                    string pasteText = Clipboard.GetText();
                    this.ViewModel.FileViewModel?.Write(pasteText);
                    break;

                case Key.X when modifiers.Ctrl:
                    string? cutText = this.ViewModel.FileViewModel?.Read();
                    if (!string.IsNullOrEmpty(cutText))
                    {
                        Clipboard.SetText(cutText);
                        this.ViewModel.FileViewModel?.Write(string.Empty);
                    }
                    break;

                case Key.Z when modifiers.Ctrl:
                    int newSelectionIndex = this.ViewModel.FileViewModel.Rope.Undo();
                    if (newSelectionIndex != -1)
                    {
                        SelectionRange selection = this.ViewModel.FileViewModel.ResetSelections();
                        selection.Update(new SelectionRange(newSelectionIndex));
                        this.ViewModel.FileViewModel.Recalculate(true);
                    }
                    break;

                case Key.F when modifiers.Ctrl:
                    this.ViewModel.OpenSearchWindow();
                    break;

                default:
                    isHandled = false;
                    break;
            }

            e.Handled = isHandled;
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            string text = e.Text;

            Trace.WriteLine("Window_TextInput - text: " + text);

            if (!string.IsNullOrEmpty(text) && !char.IsControl(text.First()))
            {
                this.ViewModel.FileViewModel?.HandlePrintableKeys(e.Text);
                e.Handled = true;
            }
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() ?? false)
            {
                this.ViewModel.Load(dialog.FileName);
            }
        }
    }
}
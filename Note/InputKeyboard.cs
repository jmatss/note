using Editor.ViewModel;
using System.Diagnostics;
using System.Windows.Input;
using static Editor.HandleInput;

namespace Note
{
    public class InputKeyboard
    {
        public static void HandleText(FileViewModel viewModel, string text)
        {
            viewModel.HandlePrintableKeys(text);
        }

        public static void HandleKey(FileViewModel viewModel, KeyboardDevice keyboard, Key key)
        {
            Modifiers modifiers = new Modifiers(keyboard);

            Trace.WriteLine("InputKeyboard_Handle - key: " + key + ", modifiers: " + modifiers);

            switch (key)
            {
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
                    viewModel.HandleSpecialKeys(key, modifiers);
                    break;

                default:
                    break;
            }
        }
    }
}

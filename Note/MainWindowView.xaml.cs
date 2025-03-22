using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

        private void MenuItem_Lsp_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.OpenLspWindow();
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Recalculates the width of the border around the window when it is maximized
        /// https://stackoverflow.com/a/61299269
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            if (sender is not Visual visual)
            {
                return;
            }

            DpiScale dpi = VisualTreeHelper.GetDpi(visual);

            int dx = GetSystemMetrics(GetSystemMetricsIndex.CXFRAME);
            int dy = GetSystemMetrics(GetSystemMetricsIndex.CYFRAME);

            int d = GetSystemMetrics(GetSystemMetricsIndex.SM_CXPADDEDBORDER);
            dx += d;
            dy += d;

            double leftBorder = dx / dpi.DpiScaleX;
            double topBorder = dy / dpi.DpiScaleY;
            this.ViewModel.WindowMaximizedBorder = new Thickness(leftBorder, topBorder, leftBorder, topBorder);
        }

        [LibraryImport("user32.dll")]
        private static partial int GetSystemMetrics(GetSystemMetricsIndex mIndex);

        private enum GetSystemMetricsIndex
        {
            CXFRAME = 32,
            CYFRAME = 33,
            SM_CXPADDEDBORDER = 92,
        }
    }
}
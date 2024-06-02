using Editor.ViewModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Editor.View
{
    public partial class TextView
    {
        public TextView()
        {
            InitializeComponent();
        }

        public TextViewModel ViewModel => base.DataContext as TextViewModel;

        public bool MouseIsCaptured { get; set; } = false;

        private void Text_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point position = this.NormalizedPosition(sender, e, "TextSpacing");
            this.ViewModel.HandleTextMouseLeftClick(position);
            this.MouseIsCaptured = Mouse.Capture(sender as IInputElement);
        }

        private void Text_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.MouseIsCaptured = false;
            Mouse.Capture(null);
        }

        private void LineNumbers_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point position = this.NormalizedPosition(sender, e, "LineNumbersSpacing");
            this.ViewModel.HandleLineNumbersMouseLeftClick(position);
        }

        private void Text_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.MouseIsCaptured)
            {
                Point position = this.NormalizedPosition(sender, e, "TextSpacing");
                Trace.WriteLine("move - " + position);
                this.ViewModel.HandleTextMouseLeftMove(position);
            }
        }

        private Point NormalizedPosition(object sender, MouseEventArgs e, string spacingResourceName)
        {
            Thickness? spacing = this.Resources[spacingResourceName] as Thickness?;
            Point position = e.GetPosition(sender as IInputElement);
            return new Point(position.X - spacing?.Left ?? 0, position.Y - spacing?.Top ?? 0);
        }

        private void MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int scrollIncrement = this.ViewModel?.Settings?.ScrollIncrement ?? 0;
            if (e.Delta > 0)
            {
                this.ViewModel.HandleMouseWheel(-scrollIncrement);
            }
            else if (e.Delta < 0)
            {
                this.ViewModel.HandleMouseWheel(scrollIncrement);
            }
            
        }
    }
}

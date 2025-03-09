using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Note.Tabs
{
    public partial class TabGroupContainerView : UserControl
    {
        public TabGroupContainerView()
        {
            InitializeComponent();
        }

        public TabGroupContainerViewModel ViewModel => (TabGroupContainerViewModel)this.DataContext;

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is TabGroupContainerViewModel vm)
            {
                vm.TabGroupAtMousePositionView = this.TabGroupAtMousePosition;
            }
        }

        private TabGroupViewModel? TabGroupAtMousePosition()
        {
            var mousePosition = Mouse.GetPosition(this);

            foreach (var node in this.ViewModel.TabGroups)
            {
                var nodeView = (FrameworkElement)this.ItemsControl.ItemContainerGenerator.ContainerFromItem(node);

                Rect nodeRect = nodeView
                    .TransformToAncestor(this)
                    .TransformBounds(new Rect(0, 0, nodeView.ActualWidth, nodeView.ActualHeight));

                if (nodeRect.Contains(mousePosition)) 
                {
                    return node;
                }
            }

            return null;
        }
    }
}

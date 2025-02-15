using Editor.ViewModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Note
{
    /// <summary>
    /// This should either contain `Children` or `Child`.
    /// There should never be a case where both exists.
    /// </summary>
    public class TabsContainerViewModel
    {
        public TabsContainerViewModel()
        {
        }

        public TabsContainerViewModel(TabsViewModel child)
        {
            this.Child = child;
        }

        public ObservableCollection<TabsContainerViewModel> Children { get; } = new ObservableCollection<TabsContainerViewModel>();

        public TabsViewModel? Child { get; }

        public Orientation Orientation { get; } = Orientation.Horizontal;

        public TabsViewModel? FocusedTabsViewModel(FileViewModel focusedFile)
        {
            if (this.Child != null && this.Child.Tabs.Contains(focusedFile))
            {
                return this.Child;
            }

            foreach (var child in this.Children)
            {
                if (child.FocusedTabsViewModel(focusedFile) is TabsViewModel vm)
                {
                    return vm;
                }
            }

            return null;
        }

        public void RunOnAllFiles(Action<FileViewModel> action)
        {
            foreach (var child in this.Children)
            {
                child.RunOnAllFiles(action);
            }

            if (this.Child != null)
            {
                foreach (var child in this.Child.Tabs)
                {
                    action.Invoke(child);
                }
            }
        }

        public void RunOnAllVisibileFiles(Action<FileViewModel> action)
        {
            foreach (var child in this.Children)
            {
                child.RunOnAllVisibileFiles(action);
            }

            if (this.Child?.Selected != null)
            {
                action.Invoke(this.Child.Selected);
            }
        }
    }
}

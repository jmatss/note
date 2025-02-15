using Editor.ViewModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Note
{
    public class TabsViewModel : INotifyPropertyChanged
    {
        private readonly Action<FileViewModel?> _onGotFocus;
        private readonly Action<FileViewModel> _onRemove;

        public TabsViewModel(ObservableCollection<FileViewModel> tabs, Action<FileViewModel?> onGotFocus, Action<FileViewModel> onRemove)
        {
            this._onGotFocus = onGotFocus;
            this._onRemove = onRemove;
            this.Tabs = tabs;
            this.SelectedIndex = 0;
        }

        public ObservableCollection<FileViewModel> Tabs { get; }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get => this._selectedIndex;
            set
            {
                this._selectedIndex = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged(nameof(this.Selected));
            }
        }

        public FileViewModel Selected => this.Tabs[this.SelectedIndex];

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Remove(int index)
        {
            this._onRemove.Invoke(this.Tabs[index]);
        }

        public void GotFocus()
        {
            this._onGotFocus.Invoke(this.Selected);
        }
    }
}

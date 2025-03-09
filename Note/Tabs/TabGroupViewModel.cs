using Editor.ViewModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Note.Tabs
{
    public class TabGroupViewModel : INotifyPropertyChanged
    {
        public Func<int>? MousePositionHeaderIndex;
        public Func<TabGroupContainerViewModel.Position?>? MousePositionDragPosition;
        private readonly Action<FileViewModel?> _onFocusedFileChanged;
        private readonly Action<TabGroupViewModel> _onRemoveTabGroup;
        private readonly Action<TabGroupViewModel> _onMouseDown;
        private readonly Action _onMouseUp;
        private readonly Action _onMouseDrag;

        public TabGroupViewModel(ObservableCollection<FileViewModel> tabs, Action<FileViewModel?> onFocusedFileChanged, Action<TabGroupViewModel> onRemoveTabGroup, Action<TabGroupViewModel> onMouseDown, Action onMouseUp, Action onMouseDrag)
        {
            this._onFocusedFileChanged = onFocusedFileChanged;
            this._onRemoveTabGroup = onRemoveTabGroup;
            this._onMouseDown = onMouseDown;
            this._onMouseUp = onMouseUp;
            this._onMouseDrag = onMouseDrag;
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

        private int _capturedMouseTabIndex = -1;
        /// <summary>
        /// If the mouse is captured in the View and the user is dragging
        /// a tab header around, this property contains the index of the tab
        /// where the tab will be moved to when the mouse is released.
        /// 
        /// NOTE: This index represents the spaces "between" the tab headings,
        ///       so it can go to one higher that the amount of tab headings.
        /// </summary>
        public int CapturedMouseTabIndex
        {
            get => this._capturedMouseTabIndex;
            set
            {
                this._capturedMouseTabIndex = value;
                this.NotifyPropertyChanged();
            }
        }

        private TabGroupContainerViewModel.Position? _dragPosition;
        /// <summary>
        /// If the mouse is captured in the View and the user is dragging
        /// a tab header over a "FileView", this position will indicate
        /// at which place in the file that the dropped tab would be
        /// moved to.
        /// </summary>
        public TabGroupContainerViewModel.Position? DragPosition
        {
            get => this._dragPosition;
            set
            {
                this._dragPosition = value;
                this.NotifyPropertyChanged();
            }
        }

        private int _rowIndex;
        public int RowIndex
        {
            get => this._rowIndex;
            set
            {
                this._rowIndex = value;
                this.NotifyPropertyChanged();
            }
        }

        public int RowEndIndex => this.RowIndex + this.RowSpan - 1;

        private int _columnIndex;
        public int ColumnIndex
        {
            get => this._columnIndex;
            set
            {
                this._columnIndex = value;
                this.NotifyPropertyChanged();
            }
        }

        public int ColumnEndIndex => this.ColumnIndex + this.ColumnSpan - 1;

        private int _rowSpan = 1;
        public int RowSpan
        {
            get => this._rowSpan;
            set
            {
                this._rowSpan = value;
                this.NotifyPropertyChanged();
            }
        }

        private int _columnSpan = 1;
        public int ColumnSpan
        {
            get => this._columnSpan;
            set
            {
                this._columnSpan = value;
                this.NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void FocusedFileChanged()
        {
            this._onFocusedFileChanged.Invoke(this.Selected);
        }

        public void MouseDown(int index, bool isMouseCaptured)
        {
            this.SelectedIndex = index;
            this.FocusedFileChanged();

            if (isMouseCaptured)
            {
                this._onMouseDown.Invoke(this);
            }
        }

        public void MouseUp()
        {
            this._onMouseUp.Invoke();
        }

        public void MouseDrag()
        {
            this._onMouseDrag.Invoke();
        }

        public void AppendTab(FileViewModel fileViewModel)
        {
            this.Tabs.Add(fileViewModel);
            this.SelectedIndex = this.Tabs.Count - 1;
        }

        public void AddTab(int index, FileViewModel fileViewModel)
        {
            this.Tabs.Insert(index, fileViewModel);
            this.SelectedIndex = index;
        }

        public bool RemoveTab(int indexToRemove)
        {
            if (this.Tabs.Count <= 1)
            {
                this._onRemoveTabGroup.Invoke(this);
                return true;
            }
            else
            {
                int nextSelectedIndex;
                if (indexToRemove <= this.SelectedIndex && indexToRemove > 0)
                {
                    nextSelectedIndex = this.SelectedIndex - 1;
                }
                else
                {
                    nextSelectedIndex = this.SelectedIndex;
                }

                this.Tabs.RemoveAt(indexToRemove);
                this.SelectedIndex = nextSelectedIndex;
                return false;
            }
        }

        public void MoveTab(int oldIndex, int newIndex)
        {
            int maxIndex = Math.Max(oldIndex, newIndex);
            int minIndex = Math.Min(oldIndex, newIndex);

            var tempTabMax = this.Tabs[maxIndex];
            var tempTabMin = this.Tabs[minIndex];

            this.Tabs.RemoveAt(maxIndex);
            this.Tabs.RemoveAt(minIndex);

            this.Tabs.Insert(minIndex, tempTabMax);
            this.Tabs.Insert(maxIndex, tempTabMin);

            this.SelectedIndex = newIndex;
        }
    }
}

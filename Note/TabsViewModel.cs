using Editor.ViewModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

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

        private int _originalCapturedMouseTabIndex = -1;
        /// <summary>
        /// If the mouse is captured in the View and the user is dragging
        /// a tab header around, this property contains the index of the tab
        /// that the user started to drag.
        /// 
        /// NOTE: This index represents the spaces "between" the tab headings,
        ///       so it can go to one higher that the amount of tab headings.
        /// </summary>
        public int OriginalCapturedMouseTabIndex
        {
            get => this._originalCapturedMouseTabIndex;
            set
            {
                this._originalCapturedMouseTabIndex = value;
                this.NotifyPropertyChanged();
            }
        }

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

        public void HandleHeaderMouseMove(List<Rect> headers, Point position)
        {
            double leftX = headers.First().X;
            double topY = headers.First().Y;
            double bottomY = headers.Last().Y + headers.Last().Height;

            int index = -1;

            for (int i = 0; i < headers.Count; i++)
            {
                Rect curHeader = headers[i];
                Rect? nextHeader = i < headers.Count - 1 ? headers[i + 1] : null;

                // If isInFirstColum: I cover everything left of me
                bool isInFirstColum = curHeader.X <= leftX;
                // If isInLastColumn: I cover everything right of me
                bool isInLastColumn = nextHeader == null || curHeader.X >= nextHeader.Value.X;
                // If isInTopRow: I cover everything above me
                bool isInTopRow = curHeader.Y <= topY;
                // If isInBottomRow: I cover everything below me
                bool isInBottomRow = curHeader.Y + curHeader.Height >= bottomY;

                double x1 = 0;
                double y1 = 0;
                double x2 = curHeader.X + curHeader.Width / 2;
                double y2 = 0;

                if (isInFirstColum || i == 0)
                {
                    x1 = double.MinValue;
                }
                else
                {
                    Rect prevHeader = headers[i - 1];
                    x1 = prevHeader.X + prevHeader.Width / 2;
                }

                if (isInTopRow)
                {
                    y1 = double.MinValue;
                }
                else
                {
                    y1 = curHeader.Y;
                }

                if (isInBottomRow)
                {
                    y2 = double.MaxValue;
                }
                else
                {
                    y2 = RowBelowY(headers, curHeader.Y, i);
                }

                if (Contains(new Point(x1, y1), new Point(x2, y2), position))
                {
                    index = i;
                    break;
                }
                else if (isInLastColumn && Contains(new Point(x2, y1), new Point(double.MaxValue, y2), position))
                {
                    index = i + 1;
                    break;
                }
            }

            if (index == -1)
            {
                throw new UnreachableException("Unable to find header mouse move index");
            }

            this.CapturedMouseTabIndex = index;
        }

        private static double RowBelowY(List<Rect> headers, double y, int index)
        {
            for (int i = index + 1; i < headers.Count; i++)
            {
                var header = headers[i];
                if (y != header.Y)
                {
                    double rowBelowY = header.Y;
                    return rowBelowY;
                }
            }

            throw new UnreachableException("isInBottomRow false, but unable to find row below");
        }

        /// <summary>
        /// Checks if the point `position` is contained inside the rectangle represented
        /// by `p1` (upper-left corner) & `p2` (bottom-right corner)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool Contains(Point p1, Point p2, Point position) 
        {
            return position.X >= p1.X && position.X <= p2.X &&
                   position.Y >= p1.Y && position.Y <= p2.Y;
        }
    }
}

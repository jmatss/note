using Editor.ViewModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Note.Tabs
{
    /// <summary>
    /// Represents a container that can contain multiple tab-groups.
    /// 
    /// The tab-groups are draw in a Grid.
    /// </summary>
    public class TabGroupContainerViewModel : INotifyPropertyChanged
    {
        public Func<TabGroupViewModel?>? TabGroupAtMousePositionView;

        private readonly ITabFocus _tabFocus;

        public enum Position
        {
            Left,
            Top,
            Right,
            Bottom,
        }

        public TabGroupContainerViewModel(ITabFocus tabFocus)
        {
            this._tabFocus = tabFocus;
        }

        public ObservableCollection<TabGroupViewModel> TabGroups { get; } = [];


        private int _rowCount;
        public int RowCount
        {
            get => this._rowCount;
            set
            {
                this._rowCount = value;
                this.NotifyPropertyChanged();
            }
        }

        private int _columnCount;
        public int ColumnCount
        {
            get => this._columnCount;
            set
            {
                this._columnCount = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Will be set to the TabsViewModel that we started the drag operation in.
        /// The `Selected` should be the tab that we are currently dragging.
        /// If this is null, there is no drag operation in process.
        /// </summary>
        public TabGroupViewModel? CapturedTabHeader { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TabGroupViewModel? FocusedTabsViewModel(FileViewModel focusedFile)
        {
            return this.TabGroups.FirstOrDefault(x => x.Tabs.Contains(focusedFile));
        }

        public void RunOnAllFiles(Action<FileViewModel> action)
        {
            foreach (FileViewModel file in this.EnumerateFiles())
            {
                action.Invoke(file);
            }
        }

        public void RunOnAllVisibileFiles(Action<FileViewModel> action)
        {
            foreach (TabGroupViewModel tabGroup in this.TabGroups)
            {
                action.Invoke(tabGroup.Selected);
            }
        }

        public void AddNewlyOpenedFile(FileViewModel? focusedFile, FileViewModel fileViewModel)
        {
            var tabGroup = focusedFile != null ? this.FocusedTabsViewModel(focusedFile) : null;
            tabGroup ??= this.TabGroups.Cast<TabGroupViewModel>().FirstOrDefault();

            if (tabGroup == null)
            {
                tabGroup = this.CreateTabGroup(fileViewModel);
                this.AddTabGroup(Position.Top, tabGroup);
            }
            else
            {
                tabGroup.AppendTab(fileViewModel);
                tabGroup.SelectedIndex = tabGroup.Tabs.Count - 1;
            }
        }

        // TODO: Remember to update FocusedFile from caller
        public void AddTabGroup(Position position, TabGroupViewModel newTabGroup, TabGroupViewModel? targetTabGroup = null)
        {
            this.TabGroups.Add(newTabGroup);

            // Edge case if no `TabsViewModel` existed before.
            if (targetTabGroup == null) // && this.Children.Count == 1
            {
                newTabGroup.RowIndex = 0;
                newTabGroup.ColumnIndex = 0;
                this.RowCount++;
                this.ColumnCount++;
                return;
            }

            bool isTop = position is Position.Top;
            bool isBottom = position is Position.Bottom;
            bool isLeft = position is Position.Left;
            bool isRight = position is Position.Right;
            bool isVertical = isTop || isBottom;
            bool isHorizontal = isLeft || isRight;

            if (isHorizontal && targetTabGroup.ColumnSpan > 1)
            {
                var newSpan = targetTabGroup.ColumnSpan / 2;
                var targetSpan = targetTabGroup.ColumnSpan / 2 + targetTabGroup.ColumnSpan % 2;

                newTabGroup.RowIndex = targetTabGroup.RowIndex;
                newTabGroup.RowSpan = targetTabGroup.RowSpan;
                newTabGroup.ColumnSpan = newSpan;
                targetTabGroup.ColumnSpan = targetSpan;

                if (isLeft)
                {
                    newTabGroup.ColumnIndex = targetTabGroup.ColumnIndex;
                    targetTabGroup.ColumnIndex = newTabGroup.ColumnIndex + newSpan;
                }
                else // (isRight)
                {
                    newTabGroup.ColumnIndex = targetTabGroup.ColumnIndex + targetSpan;
                }
            }
            else if (isVertical && targetTabGroup.RowSpan > 1)
            {
                var newSpan = targetTabGroup.RowSpan / 2;
                var targetSpan = targetTabGroup.RowSpan / 2 + targetTabGroup.RowSpan % 2;

                newTabGroup.ColumnIndex = targetTabGroup.ColumnIndex;
                newTabGroup.RowSpan = newSpan;
                newTabGroup.ColumnSpan = targetTabGroup.ColumnSpan;
                targetTabGroup.RowSpan = targetSpan;

                if (isTop)
                {
                    newTabGroup.RowIndex = targetTabGroup.RowIndex;
                    targetTabGroup.RowIndex = newTabGroup.RowIndex + newSpan;
                }
                else // (isBottom)
                {
                    newTabGroup.RowIndex = targetTabGroup.RowIndex + targetSpan;
                }
            }
            else
            {
                newTabGroup.RowIndex = isBottom ? targetTabGroup.RowEndIndex + 1 : targetTabGroup.RowIndex;
                newTabGroup.ColumnIndex = isRight ? targetTabGroup.ColumnEndIndex + 1 : targetTabGroup.ColumnIndex;
                newTabGroup.RowSpan = isHorizontal ? targetTabGroup.RowSpan : 1;
                newTabGroup.ColumnSpan = isVertical ? targetTabGroup.ColumnSpan : 1;

                if (isHorizontal)
                {
                    this.ColumnCount++;
                }
                else // (isVertical)
                {
                    this.RowCount++;
                }

                this.AdjustGridPositionsAfterAdd(position, newTabGroup);
            }
        }

        // TODO: Remember to update FocusedFile from caller
        public void RemoveTabGroup(TabGroupViewModel tabGroup)
        {
            this.TabGroups.Remove(tabGroup);

            // Edge case if we removed the last tab group
            if (this.TabGroups.Count() == 0)
            {
                this.RowCount = 0;
                this.ColumnCount = 0;
                this._tabFocus.FocusedFile = null;
                return;
            }

            // If true: The only entry in these columns, so can remove the whole columns
            bool removeCols = Enumerable.Range(tabGroup.ColumnIndex, tabGroup.ColumnSpan)
                .All(x => this.SpansWholeRow(x, tabGroup));
            // If true: The only entry in these rows, so can remove the whole rows
            bool removeRows = Enumerable.Range(tabGroup.RowIndex, tabGroup.RowSpan)
                .All(x => this.SpansWholeCol(x, tabGroup));

            if (removeCols)
            {
                foreach (TabGroupViewModel node in this.TabGroups.Where(x => x.ColumnIndex > tabGroup.ColumnEndIndex))
                {
                    node.ColumnIndex--;
                }

                this.ColumnCount -= tabGroup.ColumnSpan;
            }
            else if (removeRows)
            {
                foreach (TabGroupViewModel node in this.TabGroups.Where(x => x.RowIndex > tabGroup.RowEndIndex))
                {
                    node.RowIndex--;
                }

                this.RowCount -= tabGroup.RowSpan;
            }
            else
            {
                // There exists other tabs both vertical & horizontal relativ to this tabs item.
                // Need to find a place where the current item can be "merged" nicely with one of its neighbours.
                this.AdjustGridPositionsAfterRemove(tabGroup);
            }

            this.RemoveRedundantColumns();
            this.RemoveRedundantRows();

            this._tabFocus.FocusedFile = this.TabGroups.First().Selected;
        }

        private void AdjustGridPositionsAfterAdd(Position position, TabGroupViewModel newTabGroup)
        {
            var oldNodes = this.TabGroups.Where(x => x != newTabGroup);

            switch (position)
            {
                case Position.Left:
                    // `newTabGroup.ColumnSpan == 1` at this point
                    foreach (var node in oldNodes.Where(x => x.ColumnEndIndex >= newTabGroup.ColumnIndex && IsOnRow(newTabGroup, x)))
                    {
                        node.ColumnIndex++;
                    }

                    foreach (var node in oldNodes.Where(x => x.ColumnEndIndex >= newTabGroup.ColumnIndex && !IsOnRow(newTabGroup, x)))
                    {
                        if (node.ColumnIndex <= newTabGroup.ColumnIndex)
                        {
                            node.ColumnSpan++;
                        }
                        else
                        {
                            node.ColumnIndex++;
                        }
                    }
                    break;

                case Position.Top:
                    // `newTabGroup.RowSpan == 1` at this point
                    foreach (var node in oldNodes.Where(x => x.RowEndIndex >= newTabGroup.RowIndex && IsOnCol(newTabGroup, x)))
                    {
                        node.RowIndex++;
                    }

                    foreach (var node in oldNodes.Where(x => x.RowEndIndex >= newTabGroup.RowIndex && !IsOnCol(newTabGroup, x)))
                    {
                        if (node.RowIndex <= newTabGroup.RowIndex)
                        {
                            node.RowSpan++;
                        }
                        else
                        {
                            node.RowIndex++;
                        }
                    }
                    break;

                case Position.Right:
                    // `newTabGroup.ColumnSpan == 1` at this point
                    foreach (var node in oldNodes.Where(x => x.ColumnEndIndex >= newTabGroup.ColumnIndex && IsOnRow(newTabGroup, x)))
                    {
                        node.ColumnIndex++;
                    }

                    foreach (var node in oldNodes.Where(x => x.ColumnEndIndex >= newTabGroup.ColumnIndex - 1 && !IsOnRow(newTabGroup, x)))
                    {
                        if (node.ColumnIndex <= newTabGroup.ColumnIndex)
                        {
                            node.ColumnSpan++;
                        }
                        else
                        {
                            node.ColumnIndex++;
                        }
                    }
                    break;

                case Position.Bottom:
                    // `newTabGroup.RowSpan == 1` at this point
                    foreach (var node in oldNodes.Where(x => x.RowEndIndex >= newTabGroup.RowIndex && IsOnCol(newTabGroup, x)))
                    {
                        node.RowIndex++;
                    }

                    foreach (var node in oldNodes.Where(x => x.RowEndIndex >= newTabGroup.RowIndex - 1 && !IsOnCol(newTabGroup, x)))
                    {
                        if (node.RowIndex <= newTabGroup.RowIndex)
                        {
                            node.RowSpan++;
                        }
                        else
                        {
                            node.RowIndex++;
                        }
                    }
                    break;
            }
        }

        private void AdjustGridPositionsAfterRemove(TabGroupViewModel tabGroup)
        {
            bool isGridSplitTopLeft = tabGroup.RowIndex > 0 && this.IsGridSplit(tabGroup.RowIndex - 1, tabGroup.ColumnIndex, Position.Left);
            bool isGridSplitTopRight = tabGroup.RowIndex > 0 && this.IsGridSplit(tabGroup.RowIndex - 1, tabGroup.ColumnEndIndex, Position.Right);
            bool isGridSplitBottomLeft = tabGroup.RowEndIndex + 1 < this.RowCount && this.IsGridSplit(tabGroup.RowEndIndex + 1, tabGroup.ColumnIndex, Position.Left);
            bool isGridSplitBottomRight = tabGroup.RowEndIndex + 1 < this.RowCount && this.IsGridSplit(tabGroup.RowEndIndex + 1, tabGroup.ColumnEndIndex, Position.Right);
            bool isGridSplitLeftTop = tabGroup.ColumnIndex > 0 && this.IsGridSplit(tabGroup.RowIndex, tabGroup.ColumnIndex - 1, Position.Top);
            bool isGridSplitLeftBottom = tabGroup.ColumnIndex > 0 && this.IsGridSplit(tabGroup.RowEndIndex, tabGroup.ColumnIndex - 1, Position.Bottom);
            bool isGridSplitRightTop = tabGroup.ColumnEndIndex + 1 < this.ColumnCount && this.IsGridSplit(tabGroup.RowIndex, tabGroup.ColumnEndIndex + 1, Position.Top);
            bool isGridSplitRightBottom = tabGroup.ColumnEndIndex + 1 < this.ColumnCount && this.IsGridSplit(tabGroup.RowEndIndex, tabGroup.ColumnEndIndex + 1, Position.Bottom);

            if (isGridSplitTopLeft && isGridSplitTopRight)
            {
                // The items above will "overtake" the space that the removed tabs occupied.
                var nodes = this.TabGroups.Where(x =>
                    x.RowEndIndex == tabGroup.RowIndex - 1 &&
                    x.ColumnIndex >= tabGroup.ColumnIndex &&
                    x.ColumnEndIndex <= tabGroup.ColumnEndIndex
                );
                foreach (var node in nodes)
                {
                    node.RowSpan += tabGroup.RowSpan;
                }
            }
            else if (isGridSplitBottomLeft && isGridSplitBottomRight)
            {
                // The items below will "overtake" the space that the removed tabs occupied.
                var nodes = this.TabGroups.Where(x =>
                    x.RowIndex == tabGroup.RowEndIndex + 1 &&
                    x.ColumnIndex >= tabGroup.ColumnIndex &&
                    x.ColumnEndIndex <= tabGroup.ColumnEndIndex
                );
                foreach (var node in nodes)
                {
                    node.RowIndex -= tabGroup.RowSpan;
                    node.RowSpan += tabGroup.RowSpan;
                }
            }
            else if (isGridSplitLeftTop && isGridSplitLeftBottom)
            {
                // The items to the left will "overtake" the space that the removed tabs occupied.
                var nodes = this.TabGroups.Where(x =>
                    x.ColumnEndIndex == tabGroup.ColumnIndex - 1 &&
                    x.RowIndex >= tabGroup.RowIndex &&
                    x.RowEndIndex <= tabGroup.RowEndIndex
                );
                foreach (var node in nodes)
                {
                    node.ColumnSpan += tabGroup.ColumnSpan;
                }
            }
            else if (isGridSplitRightTop && isGridSplitRightBottom)
            {
                // The items to the right will "overtake" the space that the removed tabs occupied.
                var nodes = this.TabGroups.Where(x =>
                    x.ColumnIndex == tabGroup.ColumnEndIndex + 1 &&
                    x.RowIndex >= tabGroup.RowIndex &&
                    x.RowEndIndex <= tabGroup.RowEndIndex
                );
                foreach (var node in nodes)
                {
                    node.ColumnIndex -= tabGroup.ColumnSpan;
                    node.ColumnSpan += tabGroup.ColumnSpan;
                }
            }
            else
            {
                throw new UnreachableException("Unable to split on removal");
            }
        }

        private void RemoveRedundantRows()
        {
            foreach ((int rowIndex, int length) in this.FindRedundantRows())
            {
                foreach (var nodeOnRow in this.TabGroups.Where(x => IsOnRow(rowIndex, x)))
                {
                    nodeOnRow.RowSpan -= length;
                }

                foreach (var nodeOnRow in this.TabGroups.Where(x => x.RowIndex > rowIndex))
                {
                    nodeOnRow.RowIndex -= length;
                }

                this.RowCount -= length;
            }
        }

        private void RemoveRedundantColumns()
        {
            foreach ((int colIndex, int length) in this.FindRedundantColumns())
            {
                foreach (var nodeOnColumn in this.TabGroups.Where(x => IsOnCol(colIndex, x)))
                {
                    nodeOnColumn.ColumnSpan -= length;
                }

                foreach (var nodeOnRow in this.TabGroups.Where(x => x.ColumnIndex > colIndex))
                {
                    nodeOnRow.ColumnIndex -= length;
                }

                this.ColumnCount -= length;
            }
        }

        /// <summary>
        /// Returns a list of the redundant rows that can be merged.
        /// 
        /// The returned list is returned in in reverse order (highest index first).
        /// </summary>
        /// <returns></returns>
        private IEnumerable<(int RowIndex, int Length)> FindRedundantRows()
        {
            int[] redundantCount = new int[this.RowCount];
            foreach (var rowWithSpan in this.TabGroups.Where(x => x.RowSpan > 1))
            {
                foreach (var idx in Enumerable.Range(rowWithSpan.RowIndex, rowWithSpan.RowSpan))
                {
                    redundantCount[idx]++;
                }
            }

            var result = new List<(int, int)>();

            int i = 0;
            while (i < redundantCount.Length)
            {
                if (redundantCount[i] == this.ColumnCount)
                {
                    int span = 1;
                    while (i + span < redundantCount.Length && redundantCount[i + span] == this.ColumnCount)
                    {
                        span++;
                    }

                    if (span >= 2)
                    {
                        result.Add((i, span - 1));
                    }

                    i += span;
                }
                else
                {
                    i++;
                }
            }

            return result.Reverse<(int, int)>();
        }

        /// <summary>
        /// Returns a list of the redundant columns that can be merged.
        /// 
        /// The returned list is returned in in reverse order (highest index first).
        /// </summary>
        /// <returns></returns>
        private IEnumerable<(int ColIndex, int Length)> FindRedundantColumns()
        {
            int[] redundantCount = new int[this.ColumnCount];
            foreach (var columnWithSpan in this.TabGroups.Where(x => x.ColumnSpan > 1))
            {
                foreach (var idx in Enumerable.Range(columnWithSpan.ColumnIndex, columnWithSpan.ColumnSpan))
                {
                    redundantCount[idx]++;
                }
            }

            var result = new List<(int, int)>();

            int i = 0;
            while (i < redundantCount.Length)
            {
                if (redundantCount[i] == this.RowCount)
                {
                    int span = 1;
                    while (i + span < redundantCount.Length && redundantCount[i + span] == this.RowCount)
                    {
                        span++;
                    }

                    if (span >= 2)
                    {
                        result.Add((i, span - 1));
                    }

                    i += span;
                }
                else
                {
                    i++;
                }
            }

            return result.Reverse<(int, int)>();
        }

        public void TabHeaderMouseDown(TabGroupViewModel tabsViewModel)
        {
            this.CapturedTabHeader = tabsViewModel;
        }

        public void TabHeaderMouseUp()
        {
            if (this.CapturedTabHeader == null)
            {
                throw new InvalidOperationException("CapturedTabHeader null during mouse up");
            }

            TabGroupViewModel? source = this.CapturedTabHeader;
            int sourceIndex = source?.SelectedIndex ?? -1;

            this.ClearCaptureContext();
            this.CapturedTabHeader = null;

            if (source == null || sourceIndex == -1)
            {
                return;
            }

            TabGroupViewModel? target = this.TabGroupAtMousePositionView?.Invoke();
            if (target == null)
            {
                return;
            }

            // Will be non-null if we are dropping into the "file-view"
            var dragPosition = target.MousePositionDragPosition?.Invoke();
            // Will be non-null if we are dropping into the "tab-header-view"
            int targetIndex = target.MousePositionHeaderIndex?.Invoke() ?? -1;

            if (dragPosition != null)
            {
                var newTabGroup = this.CreateTabGroup(source.Selected);
                this.AddTabGroup(dragPosition.Value, newTabGroup, target);

                source.RemoveTab(sourceIndex);

                newTabGroup.FocusedFileChanged();
            }
            else if (targetIndex != -1)
            {
                if (source == target)
                {
                    // The `targetIndex` represents the indices between the tab headings,
                    // so decrement the index to get an index that represents a specific tab heading.
                    if (targetIndex > sourceIndex)
                    {
                        targetIndex--;
                    }

                    if (sourceIndex != targetIndex)
                    {
                        target.MoveTab(sourceIndex, targetIndex);
                    }
                }
                else // (source != target)
                {
                    FileViewModel sourceFile = source.Tabs[sourceIndex];
                    source.RemoveTab(sourceIndex);
                    target.AddTab(targetIndex, sourceFile);
                }

                target.FocusedFileChanged();
            }
        }

        public void TabHeaderMouseDrag()
        {
            if (this.CapturedTabHeader == null)
            {
                return;
            }

            TabGroupViewModel? target = this.TabGroupAtMousePositionView?.Invoke();
            if (target == null)
            {
                return;
            }

            Position? dragPosition = target.MousePositionDragPosition?.Invoke();
            if (dragPosition != null)
            {
                this.ClearCaptureContext();
                target.DragPosition = dragPosition;
                return;
            }

            int? targetIndex = target.MousePositionHeaderIndex?.Invoke();
            if (targetIndex != null)
            {
                this.ClearCaptureContext();
                target.CapturedMouseTabIndex = targetIndex.Value;
                return;
            }
        }

        private TabGroupViewModel CreateTabGroup(FileViewModel fileViewModel)
        {
            return new TabGroupViewModel(
                [fileViewModel],
                (f) => this._tabFocus.FocusedFile = f,
                this.RemoveTabGroup,
                this.TabHeaderMouseDown,
                this.TabHeaderMouseUp,
                this.TabHeaderMouseDrag
            );
        }

        private void ClearCaptureContext()
        {
            var prevCapturedTabIndices = this.TabGroups
                .Cast<TabGroupViewModel>()
                .Where(x => x.CapturedMouseTabIndex != -1);
            foreach (var tabs in prevCapturedTabIndices)
            {
                tabs.CapturedMouseTabIndex = -1;
            }

            var prevDragPoisitions = this.TabGroups
                .Cast<TabGroupViewModel>()
                .Where(x => x.DragPosition != null);
            foreach (var tabs in prevDragPoisitions)
            {
                tabs.DragPosition = null;
            }
        }

        private bool IsGridSplit(int row, int col, Position position)
        {
            TabGroupViewModel? a = this.TabGroups.Where(x => IsOnRow(row, x) && IsOnCol(col, x)).FirstOrDefault();
            TabGroupViewModel? b;

            switch (position)
            {
                case Position.Left:
                    b = this.TabGroups.Where(x => IsOnRow(row, x) && IsOnCol(col - 1, x)).FirstOrDefault();
                    break;
                case Position.Top:
                    b = this.TabGroups.Where(x => IsOnRow(row - 1, x) && IsOnCol(col, x)).FirstOrDefault();
                    break;
                case Position.Right:
                    b = this.TabGroups.Where(x => IsOnRow(row, x) && IsOnCol(col + 1, x)).FirstOrDefault();
                    break;
                case Position.Bottom:
                    b = this.TabGroups.Where(x => IsOnRow(row + 1, x) && IsOnCol(col, x)).FirstOrDefault();
                    break;
                default:
                    throw new UnreachableException("Unknown position: " + position);
            }

            return a == null || b == null || a != b;
        }

        private bool SpansWholeRow(int col, TabGroupViewModel tabGroup)
        {
            return this.TabGroups.Where(x => IsOnCol(col, x)).All(x => x == tabGroup);
        }

        private bool SpansWholeCol(int row, TabGroupViewModel tabGroup)
        {
            return this.TabGroups.Where(x => IsOnRow(row, x)).All(x => x == tabGroup);
        }

        private static bool IsOnRow(int row, TabGroupViewModel tabGroup)
        {
            return row >= tabGroup.RowIndex && row <= tabGroup.RowEndIndex;
        }

        /// <summary>
        /// Returns true if the given `source` overlaps the rows that `target` occupies.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        private static bool IsOnRow(TabGroupViewModel target, TabGroupViewModel source)
        {
            return !(source.RowEndIndex < target.RowIndex || source.RowIndex > target.RowEndIndex);
        }

        private static bool IsOnCol(int col, TabGroupViewModel tabGroup)
        {
            return col >= tabGroup.ColumnIndex && col <= tabGroup.ColumnEndIndex;
        }

        /// <summary>
        /// Returns true if the given `source` overlaps the columns that `target` occupies.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        private static bool IsOnCol(TabGroupViewModel target, TabGroupViewModel source)
        {
            return !(source.ColumnEndIndex < target.ColumnIndex || source.ColumnIndex > target.ColumnEndIndex);
        }

        private IEnumerable<FileViewModel> EnumerateFiles()
        {
            foreach (TabGroupViewModel tabGroup in this.TabGroups)
            {
                foreach (FileViewModel file in tabGroup.Tabs)
                {
                    yield return file;
                }
            }
        }
    }
}

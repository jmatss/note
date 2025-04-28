using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Note.Tabs
{
    /// <summary>
    /// Attached properties to allow us to:
    ///  - Data bind row & column-definitions
    ///  - Add GridSplitters dynamically
    /// 
    /// Adapted from:
    /// https://stackoverflow.com/a/9007442
    /// </summary>
    public class TabGridHelpers
    {
        public static readonly DependencyProperty RowCountProperty = DependencyProperty.RegisterAttached(
            "RowCount",
            typeof(int),
            typeof(TabGridHelpers),
            new PropertyMetadata(-1, RowCountChanged)
        );

        public static readonly DependencyProperty ColumnCountProperty = DependencyProperty.RegisterAttached(
            "ColumnCount",
            typeof(int),
            typeof(TabGridHelpers),
            new PropertyMetadata(-1, ColumnCountChanged)
        );

        public static int GetRowCount(DependencyObject obj)
        {
            return (int)obj.GetValue(RowCountProperty);
        }

        public static void SetRowCount(DependencyObject obj, int value)
        {
            obj.SetValue(RowCountProperty, value);
        }

        public static void RowCountChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is Grid grid && e.NewValue is int newRowCount)
            {
                int rowCountIncludingSplitters = (2 * newRowCount) - 1;
                int colCountIncludingSplitters = (2 * GetColumnCount(obj)) - 1;

                UpdateGridSplitters(grid, rowCountIncludingSplitters, colCountIncludingSplitters);

                grid.RowDefinitions.Clear();
                for (int i = 0; i < rowCountIncludingSplitters; i++)
                {
                    bool isSplitter = i % 2 == 1;
                    if (isSplitter)
                    {
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0) });
                    }
                    else
                    {
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                    }
                }
            }
        }

        public static int GetColumnCount(DependencyObject obj)
        {
            return (int)obj.GetValue(ColumnCountProperty);
        }

        public static void SetColumnCount(DependencyObject obj, int value)
        {
            obj.SetValue(ColumnCountProperty, value);
        }

        public static void ColumnCountChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is Grid grid && e.NewValue is int newColCount)
            {
                int rowCountIncludingSplitters = (2 * GetRowCount(obj)) - 1;
                int colCountIncludingSplitters = (2 * newColCount) - 1;

                UpdateGridSplitters(grid, rowCountIncludingSplitters, colCountIncludingSplitters);

                grid.ColumnDefinitions.Clear();
                for (int i = 0; i < colCountIncludingSplitters; i++)
                {
                    bool isSplitter = i % 2 == 1;
                    if (isSplitter)
                    {
                        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0) });
                    }
                    else
                    {
                        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    }
                }
            }
        }

        /// <summary>
        /// Adds/removes row and column GridSplitters according to the amount
        /// of rows and columns that the grid contains.
        /// </summary>
        /// <param name="grid">The grid to add GridSplitters to</param>
        /// <param name="rowCountIncludingSplitters">The amount of rows (including the extra rows for GridSplitters)</param>
        /// <param name="colCountIncludingSplitters">The amount of columns (including the extra columns for GridSplitters)</param>
        private static void UpdateGridSplitters(Grid grid, int rowCountIncludingSplitters, int colCountIncludingSplitters)
        {
            var oldSplittersToRemove = grid.Children.OfType<GridSplitter>().ToList();
            foreach (GridSplitter oldSplitter in oldSplittersToRemove)
            {
                InvokeRemove(grid.Children, oldSplitter);
            }

            var rowGridSplitterRanges = CollectRowGridSplitters(grid.Children.OfType<ContentPresenter>().Where(x => x.Content is TabGroupViewModel).Select(x => (TabGroupViewModel)x.Content), rowCountIncludingSplitters, colCountIncludingSplitters);
            var columnGridSplitterRanges = CollectColumnGridSplitters(grid.Children.OfType<ContentPresenter>().Where(x => x.Content is TabGroupViewModel).Select(x => (TabGroupViewModel)x.Content), rowCountIncludingSplitters, colCountIncludingSplitters);

            AddRowGridSplitters(grid, rowGridSplitterRanges, colCountIncludingSplitters);
            AddColumnGridSplitters(grid, columnGridSplitterRanges, rowCountIncludingSplitters);
        }

        /// <summary>
        /// Calculates and returns the "structure" of the row GridSplitters.
        /// </summary>
        /// <param name="tabGroups">The TabGroupViewModels contained in the Grid</param>
        /// <param name="rowCountIncludingSplitters">The amount of rows (including the extra rows for GridSplitters)</param>
        /// <param name="colCountIncludingSplitters">The amount of columns (including the extra columns for GridSplitters)</param>
        /// <returns>The SplittableRanges which represents how to create the GridSplitters</returns>
        private static IEnumerable<SplittableRange> CollectRowGridSplitters(IEnumerable<TabGroupViewModel> tabGroups, int rowCountIncludingSplitters, int colCountIncludingSplitters)
        {
            int rowCount = (rowCountIncludingSplitters + 1) / 2;
            int colCount = (colCountIncludingSplitters + 1) / 2;

            // Represents the rows where the grid splitters will be drawn.
            // The item at index 0 represents the first row, index 1 represents the second and so on.
            var splittableRanges = new List<SplittableRange>();

            for (int i = 0; i < rowCount - 1; i++)
            {
                splittableRanges.Add(new SplittableRange(0, colCount));
            }

            foreach (TabGroupViewModel tabGroup in tabGroups)
            {
                if (tabGroup.RowSpan > 1)
                {
                    for (int i = tabGroup.RowIndex; i < tabGroup.RowIndex + tabGroup.RowSpan - 1; i++)
                    {
                        splittableRanges[i].SplitOn(tabGroup.ColumnIndex, tabGroup.ColumnIndex + tabGroup.ColumnSpan);
                    }
                }
            }

            return splittableRanges;
        }

        /// <summary>
        /// Calculates and returns the "structure" of the column GridSplitters.
        /// </summary>
        /// <param name="tabGroups">The TabGroupViewModels contained in the Grid</param>
        /// <param name="rowCountIncludingSplitters">The amount of rows (including the extra rows for GridSplitters)</param>
        /// <param name="colCountIncludingSplitters">The amount of columns (including the extra columns for GridSplitters)</param>
        /// <returns>The SplittableRanges which represents how to create the GridSplitters</returns>
        private static IEnumerable<SplittableRange> CollectColumnGridSplitters(IEnumerable<TabGroupViewModel> tabGroups, int rowCountIncludingSplitters, int colCountIncludingSplitters)
        {
            int rowCount = (rowCountIncludingSplitters + 1) / 2;
            int colCount = (colCountIncludingSplitters + 1) / 2;

            // Represents the columns where the grid splitters will be drawn.
            // The item at index 0 represents the first column, index 1 represents the second and so on.
            var splittableRanges = new List<SplittableRange>();

            for (int i = 0; i < colCount - 1; i++)
            {
                splittableRanges.Add(new SplittableRange(0, rowCount));
            }

            foreach (TabGroupViewModel tabGroup in tabGroups)
            {
                if (tabGroup.ColumnSpan > 1)
                {
                    for (int i = tabGroup.ColumnIndex; i < tabGroup.ColumnIndex + tabGroup.ColumnSpan - 1; i++)
                    {
                        splittableRanges[i].SplitOn(tabGroup.RowIndex, tabGroup.RowIndex + tabGroup.RowSpan);
                    }
                }
            }

            return splittableRanges;
        }

        /// <summary>
        /// Adds GridSplitters into the `grid`.
        /// </summary>
        /// <param name="grid">The Grid to add the GridSplitters into</param>
        /// <param name="rows">The calculated positions of the GridSplitters to create</param>
        /// <param name="colCountIncludingSplitters">The amount of columns (including the extra columns for GridSplitters)</param>
        private static void AddRowGridSplitters(Grid grid, IEnumerable<SplittableRange> rows, int colCountIncludingSplitters)
        {
            foreach ((SplittableRange row, int rowIndex) in rows.Select((x, i) => (x, i)))
            {
                int rowIndexIncludingSplitters = (rowIndex * 2) + 1;

                foreach (Range columnRange in row)
                {
                    Range columnRangeIncludingSplitters = new Range(
                        columnRange.Start.Value * 2,
                        Math.Clamp(columnRange.End.Value * 2, 0, colCountIncludingSplitters)
                    );

                    var rowSplitter = new TabGridSplitter(grid)
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Center,
                        Background = Brushes.Red,
                        Height = 8,
                        Margin = new Thickness(0, -4, 0, -4),
                        BorderThickness = new Thickness(0, 4, 0, 4),
                        BorderBrush = Brushes.Transparent,
                        Focusable = false,
                    };

                    Panel.SetZIndex(rowSplitter, 1);

                    Grid.SetRow(rowSplitter, rowIndexIncludingSplitters);
                    Grid.SetColumn(rowSplitter, columnRangeIncludingSplitters.Start.Value);
                    Grid.SetColumnSpan(rowSplitter, columnRangeIncludingSplitters.End.Value - columnRangeIncludingSplitters.Start.Value);

                    InvokeAdd(grid.Children, rowSplitter);
                }
            }
        }

        /// <summary>
        /// Adds GridSplitters into the `grid`.
        /// </summary>
        /// <param name="grid">The Grid to add the GridSplitters into</param>
        /// <param name="columns">The calculated positions of the GridSplitters to create</param>
        /// <param name="rowCountIncludingSplitters">The amount of rows (including the extra rows for GridSplitters)</param>

        private static void AddColumnGridSplitters(Grid grid, IEnumerable<SplittableRange> columns, int rowCountIncludingSplitters)
        {
            foreach ((SplittableRange column, int colIndex) in columns.Select((x, i) => (x, i)))
            {
                int colIndexIncludingSplitters = (colIndex * 2) + 1;

                foreach (Range rowRange in column)
                {
                    Range rowRangeIncludingSplitters = new Range(
                        rowRange.Start.Value * 2,
                        Math.Clamp(rowRange.End.Value * 2, 0, rowCountIncludingSplitters)
                    );

                    var colSplitter = new TabGridSplitter(grid)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Background = Brushes.Red,
                        ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                        Width = 8,
                        Margin = new Thickness(-4, 0, -4, 0),
                        BorderThickness = new Thickness(4, 0, 4, 0),
                        BorderBrush = Brushes.Transparent,
                        Focusable = false,
                    };

                    Panel.SetZIndex(colSplitter, 1);

                    Grid.SetColumn(colSplitter, colIndexIncludingSplitters);
                    Grid.SetRow(colSplitter, rowRangeIncludingSplitters.Start.Value);
                    Grid.SetRowSpan(colSplitter, rowRangeIncludingSplitters.End.Value - rowRangeIncludingSplitters.Start.Value);

                    InvokeAdd(grid.Children, colSplitter);
                }
            }
        }

        /// <summary>
        /// The `UIElementCollection` have some checks on its Add/Remove functions
        /// that throws exception if we try to use them directly.
        /// 
        /// This hack instead calls the Add/Remove-Internal functions that skips
        /// the extra check. Otherwise we would not be able to add the GridSplitters
        /// dynamically.
        /// 
        /// See:
        /// https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/UIElementCollection.cs#L159
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="methodName"></param>
        /// <param name="uiElement"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private static void InvokeMethod(UIElementCollection collection, string methodName, UIElement uiElement)
        {
            var type = collection.GetType();
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method != null)
            {
                method.Invoke(collection, [uiElement]);
            }
            else
            {
                throw new InvalidOperationException("Unable to find method with name " + methodName + " in type " + type);
            }
        }

        private static void InvokeAdd(UIElementCollection collection,  UIElement uiElement)
        {
            InvokeMethod(collection, "AddInternal", uiElement);
        }

        private static void InvokeRemove(UIElementCollection collection, UIElement uiElement)
        {
            InvokeMethod(collection, "RemoveInternal", uiElement);
        }
    }
}

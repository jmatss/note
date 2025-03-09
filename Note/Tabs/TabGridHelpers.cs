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

        private static void UpdateGridSplitters(Grid grid, int rowCountIncludingSplitters, int colCountIncludingSplitters)
        {
            var oldSplittersToRemove = grid.Children.OfType<GridSplitter>().ToList();
            foreach (GridSplitter oldSplitter in oldSplittersToRemove)
            {
                InvokeRemove(grid.Children, oldSplitter);
            }

            AddRowGridSplitters(grid, rowCountIncludingSplitters, colCountIncludingSplitters);
            AddColumnGridSplitters(grid, rowCountIncludingSplitters, colCountIncludingSplitters);
        }

        private static void AddRowGridSplitters(Grid grid, int rowCountIncludingSplitters, int colCountIncludingSplitters)
        {
            for (int i = 1; i < rowCountIncludingSplitters; i += 2)
            {
                var rowSplitter = new TabGridSplitter(grid)
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.Red,
                    Height = 8,
                    Margin = new Thickness(0, -4, 0, -4),
                    BorderThickness = new Thickness(0, 4, 0, 4),
                    BorderBrush = Brushes.Transparent,
                };

                Panel.SetZIndex(rowSplitter, 1);
                Grid.SetRow(rowSplitter, i);
                Grid.SetColumnSpan(rowSplitter, colCountIncludingSplitters);

                InvokeAdd(grid.Children, rowSplitter);
            }
        }

        private static void AddColumnGridSplitters(Grid grid, int rowCountIncludingSplitters, int colCountIncludingSplitters)
        {
            for (int i = 1; i < colCountIncludingSplitters; i += 2)
            {
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
                };

                Panel.SetZIndex(colSplitter, 1);
                Grid.SetColumn(colSplitter, i);
                Grid.SetRowSpan(colSplitter, rowCountIncludingSplitters);

                InvokeAdd(grid.Children, colSplitter);
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

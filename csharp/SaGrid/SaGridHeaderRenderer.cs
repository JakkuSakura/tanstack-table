using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Declarative;
using TanStack.Table.Core;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input;

namespace SaGrid;

internal class SaGridHeaderRenderer<TData>
{
    private readonly Action<TextBox>? _onFilterFocus;

    public SaGridHeaderRenderer(Action<TextBox>? onFilterFocus = null)
    {
        _onFilterFocus = onFilterFocus;
    }
    public Control CreateHeader(SaGrid<TData> saGrid, Func<SaGrid<TData>>? gridSignalGetter = null, Func<int>? selectionSignalGetter = null)
    {
        var headerControls = new List<Control>();
        
        // Add header title rows with sortable headers
        headerControls.AddRange(saGrid.HeaderGroups.Select(headerGroup =>
            new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Children(
                    headerGroup.Headers.Select(header =>
                    {
                        var column = (Column<TData>)header.Column;
                        var border = new Border()
                            .BorderThickness(0, 0, 1, 1)
                            .BorderBrush(Brushes.LightGray)
                            .Background(Brushes.LightBlue)
                            .Padding(new Thickness(0))
                            .Width(header.Size)
                            .Height(40)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Stretch);

                        // Use a Button for reliable click handling and accessibility
                        var button = new Button
                        {
                            Background = Brushes.Transparent,
                            BorderBrush = null,
                            BorderThickness = new Thickness(0),
                            Padding = new Thickness(0),
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            VerticalContentAlignment = VerticalAlignment.Stretch,
                            Focusable = false,
                            IsTabStop = false,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Cursor = new Cursor(StandardCursorType.Hand)
                        };

                        // Reactive header label with sort indicators
                        var label = SolidAvalonia.Solid.Reactive(() =>
                        {
                            var _ = gridSignalGetter?.Invoke();
                            var __ = selectionSignalGetter?.Invoke();
                            var title = SaGridContentHelper<TData>.GetHeaderContent(header);
                            string sortSuffix = "";
                            if (column.SortDirection != null)
                            {
                                var arrow = column.SortDirection == TanStack.Table.Core.SortDirection.Ascending ? "▲" : "▼";
                                // Only show index when multi-sort is enabled (runtime toggle aware)
                                var isMulti = saGrid is SaGrid<TData> g && g.IsMultiSortEnabled();
                                var index = (isMulti && column.SortIndex.HasValue)
                                    ? $" {column.SortIndex.Value + 1}"
                                    : string.Empty;
                                sortSuffix = $" {arrow}{index}";
                            }
                            var container = new Grid
                            {
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                VerticalAlignment = VerticalAlignment.Stretch
                            };
                            var centeredTitle = new TextBlock()
                                .Text(title)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .TextAlignment(TextAlignment.Center)
                                .FontWeight(FontWeight.Bold);
                            var rightIndicator = new TextBlock()
                                .Text(sortSuffix)
                                .HorizontalAlignment(HorizontalAlignment.Right)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .Margin(new Thickness(8, 0, 8, 0));
                            container.Children.Add(centeredTitle);
                            container.Children.Add(rightIndicator);
                            return container;
                        });

                        button.Content = label;

                        // Handle modifiers for multi-sort at PointerPressed to capture KeyModifiers
                        button.PointerPressed += (s, e) =>
                        {
                            var mods = e.KeyModifiers;
                            var multiRequested = mods.HasFlag(KeyModifiers.Shift) ||
                                                 mods.HasFlag(KeyModifiers.Control) ||
                                                 mods.HasFlag(KeyModifiers.Meta);

                            var isMulti = saGrid is SaGrid<TData> g && g.IsMultiSortEnabled();
                            if (multiRequested && isMulti)
                            {
                                // Append/remove this column in the multi-sort chain
                                column.ToggleSorting();
                                e.Handled = true;
                                return;
                            }
                            // Otherwise, single-sort cycle below (handled on Click)
                        };

                        button.Click += (s, e) =>
                        {
                            // Single-sort: replace others and cycle Asc -> Desc -> None
                            var currentDir = column.SortDirection;
                            if (currentDir == null)
                            {
                                saGrid.SetSorting(new[] { new ColumnSort(column.Id, SortDirection.Ascending) });
                            }
                            else if (currentDir == SortDirection.Ascending)
                            {
                                saGrid.SetSorting(new[] { new ColumnSort(column.Id, SortDirection.Descending) });
                            }
                            else
                            {
                                saGrid.SetSorting(Array.Empty<ColumnSort>());
                            }
                        };

                        // Make the entire cell clickable
                        border.Child(button);

                        return border;
                    }).ToArray()
                )
        ));
        
        // Add filter row if column filtering is enabled
        if (saGrid.Options.EnableColumnFilters)
        {
            var filterRow = CreateFilterRow(saGrid);
            Console.WriteLine($"Adding filter row to header - Total header controls: {headerControls.Count + 1}");
            headerControls.Add(filterRow);
        }
        else
        {
            Console.WriteLine("Column filtering is disabled - no filter row will be added");
        }
        
        return new StackPanel()
            .Orientation(Orientation.Vertical)
            .Children(headerControls.ToArray());
    }

    private Control CreateFilterRow(SaGrid<TData> saGrid)
    {
        Console.WriteLine($"Creating filter row with {saGrid.VisibleLeafColumns.Count} columns");
        
        var filterControls = saGrid.VisibleLeafColumns.Select(column =>
        {
            Console.WriteLine($"Creating filter for column: {column.Id}");
            var textBox = CreateFilterTextBox(saGrid, column);
            return new Border()
                .BorderThickness(0, 0, 1, 1)
                .BorderBrush(Brushes.LightGray)
                .Background(Brushes.White)
                .Width(column.Size)
                .Height(35)
                .Padding(new Thickness(2))
                .Child(textBox);
        }).ToArray();

        // Put filter row inside a Border with a visible bottom line to suggest input area
        return new Border()
            .BorderThickness(new Thickness(0,0,0,1))
            .BorderBrush(Brushes.LightGray)
            .Child(
                new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Children(filterControls)
            );
    }

    private Control CreateFilterTextBox(SaGrid<TData> saGrid, Column<TData> column)
    {
        Console.WriteLine($"Creating TextBox for column {column.Id}");
        
        var textBox = new TextBox
        {
            Watermark = $"Filter {column.Id}...",
            Width = double.NaN, // Auto width
            Height = double.NaN, // Auto height
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Focusable = true,
            IsEnabled = true,
            AcceptsReturn = false,
            AcceptsTab = false
        };
        textBox.Margin = new Thickness(4, 4, 4, 4);
        textBox.BorderThickness = new Thickness(1);
        textBox.BorderBrush = Brushes.Gray;
        textBox.Background = Brushes.White;
        // Ensure the TextBox can receive input immediately
        textBox.TabIndex = 0;
        textBox.IsTabStop = true;
        
        Console.WriteLine($"TextBox created for {column.Id} - Focusable: {textBox.Focusable}, IsEnabled: {textBox.IsEnabled}");

        // Add multiple event handlers to debug what's happening
        textBox.GotFocus += (sender, args) =>
        {
            Console.WriteLine($"TextBox for column {column.Id} got focus");
            if (sender is TextBox tb)
            {
                _onFilterFocus?.Invoke(tb);
            }
        };

        textBox.LostFocus += (sender, args) =>
        {
            Console.WriteLine($"TextBox for column {column.Id} lost focus");
        };

        textBox.PointerPressed += (sender, args) =>
        {
            Console.WriteLine($"TextBox for column {column.Id} pointer pressed");
            // Explicitly capture focus on click to resist parent re-renders
            if (sender is TextBox tb && !tb.IsFocused)
            {
                tb.Focus();
            }
            args.Handled = false;
        };

        textBox.PointerEntered += (sender, args) =>
        {
            Console.WriteLine($"TextBox for column {column.Id} pointer entered");
        };

        textBox.KeyDown += (sender, args) =>
        {
            // Let TextBox handle keys normally; grid ignores keys from TextBox
            Console.WriteLine($"TextBox for column {column.Id} key down: {args.Key}");
        };

        textBox.TextChanging += (sender, args) =>
        {
            Console.WriteLine($"TextBox for column {column.Id} text changing");
        };

        textBox.TextChanged += (sender, args) =>
        {
            if (sender is TextBox tb)
            {
                var newValue = string.IsNullOrWhiteSpace(tb.Text) ? (object?)null : tb.Text;

                // Avoid redundant SetColumnFilter calls that can cause render loops
                var currentValue = saGrid.State.ColumnFilters?.Filters
                    .FirstOrDefault(f => f.Id == column.Id)?.Value;

                var equals = (currentValue == null && newValue == null) ||
                             (currentValue != null && newValue != null &&
                              string.Equals(currentValue.ToString(), newValue.ToString(), StringComparison.Ordinal));

                if (!equals)
                {
                    Console.WriteLine($"Filter changed for column {column.Id}: '{currentValue}' -> '{newValue}'");
                    saGrid.SetColumnFilter(column.Id, newValue);
                }
            }
        };

        // Initialize TextBox with current filter value (if any)
        var currentFilter = saGrid.State.ColumnFilters?.Filters.FirstOrDefault(f => f.Id == column.Id)?.Value?.ToString();
        if (!string.IsNullOrEmpty(currentFilter) && textBox.Text != currentFilter)
        {
            // Set Text without firing TextChanged loop by checking inequality
            textBox.Text = currentFilter;
        }

        return textBox;
    }
}

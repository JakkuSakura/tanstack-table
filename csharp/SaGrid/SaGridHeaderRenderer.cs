using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Declarative;
using TanStack.Table.Core;
using Avalonia;

namespace SaGrid;

internal class SaGridHeaderRenderer<TData>
{
    public Control CreateHeader(SaGrid<TData> saGrid)
    {
        var headerControls = new List<Control>();
        
        // Add header title rows
        headerControls.AddRange(saGrid.HeaderGroups.Select(headerGroup =>
            new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Children(
                    headerGroup.Headers.Select(header =>
                        new Border()
                            .BorderThickness(0, 0, 1, 1)
                            .BorderBrush(Brushes.LightGray)
                            .Background(Brushes.LightBlue)
                            .Width(header.Size)
                            .Height(40)
                            .Child(
                                new TextBlock()
                                    .Text(SaGridContentHelper<TData>.GetHeaderContent(header))
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .FontWeight(FontWeight.Bold)
                            )
                    ).ToArray()
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
        };

        textBox.LostFocus += (sender, args) =>
        {
            Console.WriteLine($"TextBox for column {column.Id} lost focus");
        };

        textBox.PointerPressed += (sender, args) =>
        {
            Console.WriteLine($"TextBox for column {column.Id} pointer pressed");
            // Do not mark handled here; let TextBox process the event normally
            // The grid container no longer steals focus on pointer presses.
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

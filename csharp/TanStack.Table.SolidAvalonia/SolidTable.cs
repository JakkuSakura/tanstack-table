using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Declarative;
using SolidAvalonia;
using TanStack.Table.Core;
using static SolidAvalonia.Solid;
using Avalonia.LogicalTree;
using Avalonia;

namespace TanStack.Table.SolidAvalonia;

public class SolidTable<TData> : Component
{
    private readonly TableOptions<TData> _options;
    private Table<TData>? _table;
    private (Func<Table<TData>>, Action<Table<TData>>)? _tableSignal;
    private Table<TData>? _externalTable;
    private (Func<int>, Action<int>)? _selectionSignal;

    public Table<TData> Table => _tableSignal?.Item1() ?? throw new InvalidOperationException("Table not initialized. Call Build() first.");

    public SolidTable(TableOptions<TData> options, Table<TData>? externalTable = null) : base(true)
    {
        _options = options;
        _externalTable = externalTable;
        OnCreatedCore(); // 推送 reactive owner
        Initialize(); // 触发 Build()
    }

    protected override object Build()
    {
        Console.WriteLine("SolidTable.Build invoked");
        // Initialize table and signals here in the proper component lifecycle
        if (_table == null)
        {
            _table = _externalTable ?? new Table<TData>(_options);
            _tableSignal = CreateSignal(_table);
            _selectionSignal = CreateSignal(0); // Signal for cell selection count

            // Extract the signal setter for use in the callback
            var signalSetter = _tableSignal.Value.Item2;

            // Subscribe to table state changes to trigger UI updates
            var originalOnStateChange = _options.OnStateChange;
            
            // Update the table options to include state change callback
            var newOptions = _options with
            {
                OnStateChange = state =>
                {
                    Console.WriteLine($"Table state changed - triggering reactive update");
                    originalOnStateChange?.Invoke(state);
                    // Force signal update to trigger reactive components
                    signalSetter(_table);
                }
            };

            // If this is not an external table, recreate it with the new options
            if (_externalTable == null)
            {
                _table = new Table<TData>(newOptions);
            }
            else
            {
                // For external tables, the state change callback is handled through the original options
                // We'll need to recreate the external table with new options if we want to change the callback
                _table = new Table<TData>(newOptions with { Data = _table.Options.Data, Columns = _table.Options.Columns });
            }
        }

        return Reactive(() =>
        {
            var table = Table; // Access the reactive signal

            var mainBorder = new Border()
                .BorderThickness(1)
                .BorderBrush(Brushes.Gray)
                .Child(
                    new StackPanel()
                        .Children(
                            // Header
                            CreateHeader(table),
                            // Body
                            CreateBody(table),
                            // Footer (optional)
                            CreateFooter(table)
                        )
                );

            // Add keyboard navigation for cell selection
            if (table is SaGrid<TData> saGrid && saGrid.Options.EnableCellSelection)
            {
                // Set up the UI update callback to trigger reactive updates
                var signalSetter = _tableSignal?.Item2;
                var selectionSetter = _selectionSignal?.Item2;
                
                var updateCounter = 0;
                saGrid.SetUIUpdateCallback(() =>
                {
                    updateCounter++;
                    
                    // Optional debug logging (uncomment for debugging)
                    // var cellSelection = saGrid.State.CellSelection;
                    // var selectedCount = cellSelection?.SelectedCells.Count ?? 0;
                    // var activeCell = saGrid.GetActiveCell();
                    // var activeCellInfo = activeCell != null ? $"({activeCell.RowIndex},{activeCell.ColumnId})" : "None";
                    // Console.WriteLine($"UI callback #{updateCounter}: TotalSelected: {selectedCount}, ActiveCell: {activeCellInfo}");
                    
                    // Update both signals to trigger reactive rebuilds
                    // Use a changing value to force reactive updates
                    signalSetter?.Invoke(saGrid);
                    selectionSetter?.Invoke(updateCounter); // Use counter instead of selectedCount to force change
                });

                mainBorder.Focusable = true;
                mainBorder.KeyDown += (sender, e) =>
                {
                    try
                    {
                        var direction = e.Key switch
                        {
                            Avalonia.Input.Key.Up => SaGrid<TData>.CellNavigationDirection.Up,
                            Avalonia.Input.Key.Down => SaGrid<TData>.CellNavigationDirection.Down,
                            Avalonia.Input.Key.Left => SaGrid<TData>.CellNavigationDirection.Left,
                            Avalonia.Input.Key.Right => SaGrid<TData>.CellNavigationDirection.Right,
                            _ => (SaGrid<TData>.CellNavigationDirection?)null
                        };

                        if (direction.HasValue)
                        {
                            Console.WriteLine($"Keyboard navigation: {direction.Value}");
                            saGrid.NavigateCell(direction.Value);
                            // UI update callback will be triggered automatically
                            e.Handled = true;
                        }
                        else if (e.Key == Avalonia.Input.Key.C && e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control))
                        {
                            // Ctrl+C to copy selected cells
                            var copiedText = saGrid.CopySelectedCells();
                            if (!string.IsNullOrEmpty(copiedText))
                            {
                                Console.WriteLine($"Copied to clipboard:\n{copiedText}");
                                // In a real app, you'd copy to the system clipboard
                            }
                            e.Handled = true;
                        }
                        else if (e.Key == Avalonia.Input.Key.Escape)
                        {
                            // Escape to clear selection
                            saGrid.ClearCellSelection();
                            // UI update callback will be triggered automatically
                            e.Handled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error handling keyboard navigation: {ex.Message}");
                    }
                };

                // Focus the border when clicked to enable keyboard navigation
                mainBorder.PointerPressed += (sender, e) =>
                {
                    mainBorder.Focus();
                };
            }

            return mainBorder;
        });
    }

    private Control CreateHeader(Table<TData> table)
    {
        var headerControls = new List<Control>();
        
        // Add header title rows
        headerControls.AddRange(table.HeaderGroups.Select(headerGroup =>
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
                                    .Text(GetHeaderContent(header))
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .FontWeight(FontWeight.Bold)
                            )
                    ).ToArray()
                )
        ));
        
        // Add filter row if column filtering is enabled
        if (table.Options.EnableColumnFilters)
        {
            headerControls.Add(CreateFilterRow(table));
        }
        
        return new StackPanel()
            .Orientation(Orientation.Vertical)
            .Children(headerControls.ToArray());
    }

    private Control CreateFilterRow(Table<TData> table)
    {
        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Children(
                table.VisibleLeafColumns.Select(column =>
                    new Border()
                        .BorderThickness(0, 0, 1, 1)
                        .BorderBrush(Brushes.LightGray)
                        .Background(Brushes.White)
                        .Width(column.Size)
                        .Height(35)
                        .Child(
                            CreateFilterTextBox(table, column)
                        )
                ).ToArray()
            );
    }

    private Control CreateFilterTextBox(Table<TData> table, Column<TData> column)
    {
        var textBox = new TextBox()
        {
            Watermark = $"Filter {column.Id}...",
            Margin = new Thickness(4),
            FontSize = 12,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };

        textBox.TextChanged += (sender, args) =>
        {
            if (table is SaGrid<TData> saGrid)
            {
                var filterValue = string.IsNullOrWhiteSpace(textBox.Text) ? (object?)null : textBox.Text;
                saGrid.SetColumnFilter(column.Id, filterValue);
            }
        };

        return textBox;
    }

    private Control CreateBody(Table<TData> table)
    {
        return new ScrollViewer()
            .Content(
                new StackPanel()
                    .Orientation(Orientation.Vertical)
                    .Children(
                        table.RowModel.Rows.Select(row =>
                            CreateRow(table, row)
                        ).ToArray()
                    )
            );
    }

    private Control CreateRow(Table<TData> table, Row<TData> row)
    {
        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Background(row.Index % 2 == 0 ? Brushes.White : Brushes.AliceBlue)
            .Children(
                table.VisibleLeafColumns.Select(column =>
                    CreateCell(table, row, column)
                ).ToArray()
            );
    }

    private Control CreateCell(Table<TData> table, Row<TData> row, Column<TData> column)
    {
        // For SaGrid with cell selection, create a reactive cell
        if (table is SaGrid<TData> saGrid && saGrid.Options.EnableCellSelection)
        {
            return CreateReactiveCell(saGrid, row, column);
        }

        // For regular table, create a simple cell
        return new Border()
            .BorderThickness(0, 0, 1, 1)
            .BorderBrush(Brushes.LightGray)
            .Background(GetCellBackground(false, false, row.Index))
            .Width(column.Size)
            .Height(35)
            .Child(
                new TextBlock()
                    .Text(GetCellContent(row, column))
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Margin(8, 0)
            );
    }

    private Control CreateReactiveCell(SaGrid<TData> saGrid, Row<TData> row, Column<TData> column)
    {
        return Reactive(() =>
        {
            // Access the reactive signals to trigger updates - the selection signal is the key dependency
            var updateCounter = _selectionSignal?.Item1() ?? 0; // This triggers on selection changes
            var currentTable = Table; // This is the reactive table signal
            
            // Access selection state through the reactive table signal instead of saGrid directly
            var currentSaGrid = currentTable as SaGrid<TData>;
            var isSelected = currentSaGrid?.IsCellSelected(row.Index, column.Id) ?? false;
            var activeCell = currentSaGrid?.GetActiveCell();
            var isActiveCell = activeCell?.RowIndex == row.Index && activeCell?.ColumnId == column.Id;
            
            // Optional debug logging (uncomment for debugging)
            // if (updateCounter > 0 && (isSelected || isActiveCell))
            // {
            //     Console.WriteLine($"REACTIVE EVAL: Cell ({row.Index},{column.Id}) - updateCounter={updateCounter}, Selected={isSelected}, Active={isActiveCell}");
            // }
            
            // Optional detailed debug logging (uncomment for debugging)
            // if (isSelected || isActiveCell)
            // {
            //     var cellSelection = currentSaGrid?.State.CellSelection;
            //     var selectedCellsCount = cellSelection?.SelectedCells?.Count ?? 0;
            //     var selectedCellsList = cellSelection?.SelectedCells?.Select(c => $"({c.RowIndex},{c.ColumnId})").ToArray() ?? new string[0];
            //     var selectedCellsStr = string.Join(",", selectedCellsList);
            //     Console.WriteLine($"*** SELECTED/ACTIVE cell: Row {row.Index}, Col {column.Id}, Selected: {isSelected}, Active: {isActiveCell}, UpdateCounter: {updateCounter}, SelectedCells: [{selectedCellsStr}]");
            // }

            // Determine cell styling based on current selection state
            var cellBackground = GetCellBackground(isSelected, isActiveCell, row.Index);
            var cellBorderBrush = isActiveCell ? Brushes.Blue : (isSelected ? Brushes.DarkBlue : Brushes.LightGray);
            var cellBorderThickness = isActiveCell ? 3 : (isSelected ? 2 : 1);

            var cellBorder = new Border()
                .BorderThickness(0, 0, cellBorderThickness, cellBorderThickness)
                .BorderBrush(cellBorderBrush)
                .Background(cellBackground)
                .Width(column.Size)
                .Height(35)
                .Child(
                    new TextBlock()
                        .Text(GetCellContent(row, column))
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Margin(8, 0)
                        .Foreground(isSelected || isActiveCell ? Brushes.White : Brushes.Black)
                );

            // Add click handling
            cellBorder.PointerPressed += (sender, e) =>
            {
                try
                {
                    var isCtrlPressed = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control);
                    Console.WriteLine($"Cell clicked: Row {row.Index}, Column {column.Id}, Ctrl: {isCtrlPressed}");
                    
                    saGrid.SelectCell(row.Index, column.Id, isCtrlPressed);
                    // UI update callback will be triggered automatically by saGrid.SelectCell()
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling cell click: {ex.Message}");
                }
            };

            // Add hover effect (only for unselected cells)
            if (!isSelected && !isActiveCell)
            {
                cellBorder.PointerEntered += (sender, e) =>
                {
                    cellBorder.Background = Brushes.LightSteelBlue;
                };

                cellBorder.PointerExited += (sender, e) =>
                {
                    cellBorder.Background = GetCellBackground(false, false, row.Index);
                };
            }

            return cellBorder;
        });
    }

    private IBrush GetCellBackground(bool isSelected, bool isActiveCell, int rowIndex)
    {
        if (isActiveCell)
            return Brushes.Orange; // More visible active cell
        if (isSelected)
            return Brushes.LightBlue; // More visible selected cell
        return rowIndex % 2 == 0 ? Brushes.White : Brushes.AliceBlue;
    }

    private Control CreateFooter(Table<TData> table)
    {
        if (!table.FooterGroups.Any())
            return new Panel(); // Empty footer

        return new StackPanel()
            .Orientation(Orientation.Vertical)
            .Children(
                table.FooterGroups.Select(footerGroup =>
                    new StackPanel()
                        .Orientation(Orientation.Horizontal)
                        .Children(
                            footerGroup.Headers.Select(header =>
                                new Border()
                                    .BorderThickness(0, 1, 1, 0)
                                    .BorderBrush(Brushes.LightGray)
                                    .Background(Brushes.LightGray)
                                    .Width(header.Size)
                                    .Height(35)
                                    .Child(
                                        new TextBlock()
                                            .Text(GetFooterContent(header))
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .FontWeight(FontWeight.Bold)
                                    )
                            ).ToArray()
                        )
                ).ToArray()
            );
    }

    private string GetHeaderContent(IHeader<TData> header)
    {
        return header.Column.ColumnDef.Header?.ToString() ?? header.Column.Id;
    }

    private string GetFooterContent(IHeader<TData> header)
    {
        return header.Column.ColumnDef.Footer?.ToString() ?? "";
    }

    private string GetCellContent(Row<TData> row, Column<TData> column)
    {
        try
        {
            var cell = row.GetCell(column.Id);
            if (cell == null)
            {
                Console.WriteLine($"DEBUG cell null row={row.Id} colId={column.Id}");
                return "";
            }

            var v = cell.Value;
            Console.WriteLine($"DEBUG cell row={row.Id} col={column.Id} value={(v==null?"<null>":v)}");
            return v?.ToString() ?? "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG GetCellContent exception row={row.Id} colId={column.Id}: {ex}");
            return "";
        }
    }

    // Reactive helper methods for common table operations
    public void SetSorting(string columnId, SortDirection? direction = null)
    {
        var column = Table.GetColumn(columnId);
        column?.ToggleSorting(direction);
    }

    public void SetFilter(string columnId, object? value)
    {
        var column = Table.GetColumn(columnId);
        column?.SetFilterValue(value);
    }

    public void SetGlobalFilter(object? value)
    {
        Table.SetState(state => state with
        {
            GlobalFilter = value != null ? new GlobalFilterState(value) : null
        });
    }

    public void ToggleRowSelection(string rowId)
    {
        var row = Table.GetRow(rowId);
        row?.ToggleSelected();
    }

    public void SetPageIndex(int pageIndex)
    {
        var currentPagination = Table.State.Pagination ?? new PaginationState();
        Table.SetState(state => state with
        {
            Pagination = currentPagination with { PageIndex = pageIndex }
        });
    }

    public void SetPageSize(int pageSize)
    {
        var currentPagination = Table.State.Pagination ?? new PaginationState();
        Table.SetState(state => state with
        {
            Pagination = currentPagination with { PageSize = pageSize }
        });
    }
}

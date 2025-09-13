using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Declarative;
using SolidAvalonia;
using TanStack.Table.Core;
using static SolidAvalonia.Solid;
using Avalonia.LogicalTree;

namespace TanStack.Table.SolidAvalonia;

public class SolidTable<TData> : Component
{
    private readonly TableOptions<TData> _options;
    private Table<TData>? _table;
    private (Func<Table<TData>>, Action<Table<TData>>)? _tableSignal;
    private Table<TData>? _externalTable;

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

            // Extract the signal setter for use in the callback
            var signalSetter = _tableSignal.Value.Item2;

            // Subscribe to table state changes to trigger UI updates
            var originalOnStateChange = _options.OnStateChange;
            var newOptions = _options with
            {
                OnStateChange = state =>
                {
                    originalOnStateChange?.Invoke(state);
                    // Force signal update to trigger reactive components
                    signalSetter(_table);
                }
            };
        }

        return Reactive(() =>
        {
            var table = Table; // Access the reactive signal

            return new Border()
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
        });
    }

    private Control CreateHeader(Table<TData> table)
    {
        return new StackPanel()
            .Orientation(Orientation.Vertical)
            .Children(
                table.HeaderGroups.Select(headerGroup =>
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
                ).ToArray()
            );
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
                    new Border()
                        .BorderThickness(0, 0, 1, 1)
                        .BorderBrush(Brushes.LightGray)
                        .Width(column.Size)
                        .Height(35)
                        .Child(
                            new TextBlock()
                                .Text(GetCellContent(row, column))
                                .VerticalAlignment(VerticalAlignment.Center)
                                .Margin(8, 0)
                        )
                ).ToArray()
            );
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

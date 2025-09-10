using System.Linq.Expressions;

namespace TanStack.Table.Core;

public delegate T Updater<T>(T value);
public delegate T AccessorFn<TData, T>(TData data);
public delegate TResult ColumnDefTemplate<TData, TValue, TResult>(ColumnDef<TData, TValue> columnDef);

public record TableOptions<TData>
{
    public required IEnumerable<TData> Data { get; init; }
    public required IReadOnlyList<ColumnDef<TData>> Columns { get; init; }
    public TableState<TData>? State { get; init; }
    public Action<TableState<TData>>? OnStateChange { get; init; }
    public Func<TData[], RowModel<TData>>? GetCoreRowModel { get; init; }
    public Func<Table<TData>, RowModel<TData>>? GetFilteredRowModel { get; init; }
    public Func<Table<TData>, RowModel<TData>>? GetSortedRowModel { get; init; }
    public Func<Table<TData>, RowModel<TData>>? GetGroupedRowModel { get; init; }
    public Func<Table<TData>, RowModel<TData>>? GetExpandedRowModel { get; init; }
    public Func<Table<TData>, RowModel<TData>>? GetPaginationRowModel { get; init; }
    public bool EnableColumnFilters { get; init; } = true;
    public bool EnableGlobalFilter { get; init; } = true;
    public bool EnableSorting { get; init; } = true;
    public bool EnableMultiSort { get; init; } = true;
    public bool EnableGrouping { get; init; } = false;
    public bool EnableExpanding { get; init; } = false;
    public bool EnableRowSelection { get; init; } = false;
    public bool EnableColumnResizing { get; init; } = false;
    public bool EnableColumnReordering { get; init; } = false;
    public bool EnableColumnPinning { get; init; } = false;
    public bool EnablePagination { get; init; } = false;
    public Dictionary<string, object>? Meta { get; init; }
}

public record TableState<TData>
{
    public ColumnFiltersState? ColumnFilters { get; init; }
    public GlobalFilterState? GlobalFilter { get; init; }
    public SortingState? Sorting { get; init; }
    public GroupingState? Grouping { get; init; }
    public ExpandedState? Expanded { get; init; }
    public RowSelectionState? RowSelection { get; init; }
    public ColumnOrderState? ColumnOrder { get; init; }
    public ColumnPinningState? ColumnPinning { get; init; }
    public ColumnSizingState? ColumnSizing { get; init; }
    public ColumnVisibilityState? ColumnVisibility { get; init; }
    public PaginationState? Pagination { get; init; }
}

public record RowModel<TData>
{
    public required IReadOnlyList<Row<TData>> Rows { get; init; }
    public required IReadOnlyList<Row<TData>> FlatRows { get; init; }
    public required IReadOnlyDictionary<string, Row<TData>> RowsById { get; init; }
}

public enum SortDirection
{
    Ascending,
    Descending
}

public record SortingState(List<ColumnSort> Columns = null!)
{
    public List<ColumnSort> Columns { get; init; } = Columns ?? new();
}

public record ColumnSort(string Id, SortDirection Direction);

public record ColumnFiltersState(List<ColumnFilter> Filters = null!)
{
    public List<ColumnFilter> Filters { get; init; } = Filters ?? new();
}

public record ColumnFilter(string Id, object? Value);

public record GlobalFilterState(object? Value);

public record GroupingState(List<string> Groups = null!)
{
    public List<string> Groups { get; init; } = Groups ?? new();
}

public record ExpandedState(Dictionary<string, bool> Items = null!)
{
    public Dictionary<string, bool> Items { get; init; } = Items ?? new();
}

public record RowSelectionState(Dictionary<string, bool> Items = null!)
{
    public Dictionary<string, bool> Items { get; init; } = Items ?? new();
}

public record ColumnOrderState(List<string> Order = null!)
{
    public List<string> Order { get; init; } = Order ?? new();
}

public record ColumnPinningState
{
    public IReadOnlyList<string>? Left { get; init; }
    public IReadOnlyList<string>? Right { get; init; }
}

public record ColumnSizingState(Dictionary<string, double> Items = null!)
{
    public Dictionary<string, double> Items { get; init; } = Items ?? new();
}

public record ColumnVisibilityState(Dictionary<string, bool> Items = null!)
{
    public Dictionary<string, bool> Items { get; init; } = Items ?? new();
}

public record PaginationState
{
    public int PageIndex { get; init; } = 0;
    public int PageSize { get; init; } = 10;
}

public abstract record ColumnDef<TData>
{
    public string? Id { get; init; }
    public object? Header { get; init; }
    public object? Footer { get; init; }
    public bool? EnableSorting { get; init; }
    public bool? EnableColumnFilter { get; init; }
    public bool? EnableGlobalFilter { get; init; }
    public bool? EnableGrouping { get; init; }
    public bool? EnableResizing { get; init; }
    public int? Size { get; init; }
    public int? MinSize { get; init; }
    public int? MaxSize { get; init; }
    public Dictionary<string, object>? Meta { get; init; }
}

public record ColumnDef<TData, TValue> : ColumnDef<TData>
{
    public AccessorFn<TData, TValue>? AccessorFn { get; init; }
    public string? AccessorKey { get; init; }
    public object? Cell { get; init; }
    public Func<TValue, TValue, int>? SortingFn { get; init; }
    public Func<Row<TData>, string, TValue, bool>? FilterFn { get; init; }
    public Func<IEnumerable<TValue>, TValue>? AggregationFn { get; init; }
}

public record GroupColumnDef<TData> : ColumnDef<TData>
{
    public required IReadOnlyList<ColumnDef<TData>> Columns { get; init; }
}
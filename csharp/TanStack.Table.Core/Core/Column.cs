using System.Linq.Expressions;

namespace TanStack.Table.Core;

public class Column<TData> : IColumn<TData>
{
    private readonly Table<TData> _table;
    private readonly List<Column<TData>> _columns = new();

    public string Id { get; }
    public ColumnDef<TData> ColumnDef { get; }
    public int Depth { get; }
    public Column<TData>? Parent { get; }
    public IReadOnlyList<IColumn<TData>> Columns => _columns.Cast<IColumn<TData>>().ToList().AsReadOnly();
    public IReadOnlyList<IColumn<TData>> FlatColumns => GetFlatColumns();
    public IReadOnlyList<IColumn<TData>> LeafColumns => GetLeafColumns();

    public bool IsVisible => GetVisibility();
    public bool CanSort => GetCanSort();
    public bool CanFilter => GetCanFilter();
    public bool CanGroup => GetCanGroup();
    public bool CanResize => GetCanResize();

    private bool GetVisibility()
    {
        var visibilityState = _table.State.ColumnVisibility ?? new ColumnVisibilityState();
        return visibilityState.GetValueOrDefault(Id, true);
    }
    

    public SortDirection? SortDirection => GetSortDirection();
    public int? SortIndex => GetSortIndex();
    public bool IsFiltered => GetIsFiltered();
    public object? FilterValue => GetFilterValue();
    public bool IsGrouped => GetIsGrouped();
    public int? GroupIndex => GetGroupIndex();
    public double Size => GetSize();
    public bool IsPinned => GetIsPinned();
    public string? PinnedPosition => GetPinnedPosition();

    public Column(Table<TData> table, ColumnDef<TData> columnDef, Column<TData>? parent, int depth)
    {
        _table = table;
        ColumnDef = columnDef;
        Parent = parent;
        Depth = depth;

        Id = columnDef.Id ?? GenerateId(columnDef, parent);

        parent?.AddChild(this);
    }

    private string GenerateId(ColumnDef<TData> columnDef, Column<TData>? parent)
    {
        if (columnDef is ColumnDef<TData, object> typedColumnDef && typedColumnDef.AccessorKey != null)
            return typedColumnDef.AccessorKey;

        var parentId = parent?.Id ?? "";
        var index = parent?._columns.Count ?? 0;
        return $"{parentId}_{index}";
    }

    private void AddChild(Column<TData> child)
    {
        _columns.Add(child);
    }

    private IReadOnlyList<IColumn<TData>> GetFlatColumns()
    {
        var result = new List<IColumn<TData>> { this };
        foreach (var column in _columns)
        {
            result.AddRange(column.FlatColumns);
        }
        return result.AsReadOnly();
    }

    private IReadOnlyList<IColumn<TData>> GetLeafColumns()
    {
        if (!_columns.Any())
            return new List<IColumn<TData>> { this }.AsReadOnly();

        var result = new List<IColumn<TData>>();
        foreach (var column in _columns)
        {
            result.AddRange(column.LeafColumns);
        }
        return result.AsReadOnly();
    }


    private bool GetCanSort()
    {
        return ColumnDef.EnableSorting ?? _table.Options.EnableSorting;
    }

    private bool GetCanFilter()
    {
        return ColumnDef.EnableColumnFilter ?? _table.Options.EnableColumnFilters;
    }

    private bool GetCanGroup()
    {
        return ColumnDef.EnableGrouping ?? _table.Options.EnableGrouping;
    }

    private bool GetCanResize()
    {
        return ColumnDef.EnableResizing ?? _table.Options.EnableColumnResizing;
    }

    private SortDirection? GetSortDirection()
    {
        var sortingState = _table.State.Sorting;
        if (sortingState == null) return null;

        var sort = sortingState.FirstOrDefault(s => s.Id == Id);
        return sort?.Direction;
    }

    private int? GetSortIndex()
    {
        var sortingState = _table.State.Sorting;
        if (sortingState == null) return null;

        var index = sortingState.FindIndex(s => s.Id == Id);
        return index >= 0 ? index : null;
    }

    private bool GetIsFiltered()
    {
        var filtersState = _table.State.ColumnFilters;
        return filtersState?.Any(f => f.Id == Id) ?? false;
    }

    private object? GetFilterValue()
    {
        var filtersState = _table.State.ColumnFilters;
        return filtersState?.FirstOrDefault(f => f.Id == Id)?.Value;
    }

    private bool GetIsGrouped()
    {
        var groupingState = _table.State.Grouping;
        return groupingState?.Contains(Id) ?? false;
    }

    private int? GetGroupIndex()
    {
        var groupingState = _table.State.Grouping;
        if (groupingState == null) return null;

        var index = groupingState.FindIndex(g => g == Id);
        return index >= 0 ? index : null;
    }

    private double GetSize()
    {
        var sizingState = _table.State.ColumnSizing;
        return sizingState?.GetValueOrDefault(Id, ColumnDef.Size ?? 150) ?? 150;
    }

    private bool GetIsPinned()
    {
        var pinningState = _table.State.ColumnPinning;
        return pinningState?.Left?.Contains(Id) == true ||
               pinningState?.Right?.Contains(Id) == true;
    }

    private string? GetPinnedPosition()
    {
        var pinningState = _table.State.ColumnPinning;
        if (pinningState?.Left?.Contains(Id) == true) return "left";
        if (pinningState?.Right?.Contains(Id) == true) return "right";
        return null;
    }

    public void ToggleSorting(SortDirection? direction = null)
    {
        var currentSorting = _table.State.Sorting ?? new SortingState();
        var newSorting = currentSorting;

        var existingSort = newSorting.FirstOrDefault(s => s.Id == Id);

        if (existingSort != null)
        {
            if (direction.HasValue)
            {
                newSorting = newSorting.Where(s => s.Id != Id);
                newSorting = newSorting.Add(new ColumnSort(Id, direction.Value));
            }
            else
            {
                var currentDir = existingSort.Direction;
                if (currentDir == TanStack.Table.Core.SortDirection.Ascending)
                {
                    newSorting = newSorting.Where(s => s.Id != Id);
                    newSorting = newSorting.Add(new ColumnSort(Id, TanStack.Table.Core.SortDirection.Descending));
                }
                else
                {
                    newSorting = newSorting.Where(s => s.Id != Id);
                }
            }
        }
        else
        {
            var sortDir = direction ?? TanStack.Table.Core.SortDirection.Ascending;
            if (_table.Options.EnableMultiSort)
                newSorting = newSorting.Add(new ColumnSort(Id, sortDir));
            else
            {
                newSorting = new SortingState();
                newSorting = newSorting.Add(new ColumnSort(Id, sortDir));
            }
        }

        _table.SetState(state => state with { Sorting = newSorting });
    }

    public void ClearSorting()
    {
        var currentSorting = _table.State.Sorting ?? new SortingState();
        var newSorting = currentSorting.Where(s => s.Id != Id);
        _table.SetState(state => state with { Sorting = newSorting });
    }

    public void SetFilterValue(object? value)
    {
        var currentFilters = _table.State.ColumnFilters ?? new ColumnFiltersState();
        var newFilters = currentFilters.Where(f => f.Id != Id);

        if (value != null)
            newFilters = newFilters.Add(new ColumnFilter(Id, value));

        _table.SetState(state => state with { ColumnFilters = newFilters });
    }

    public void ToggleGrouping()
    {
        var currentGrouping = _table.State.Grouping ?? new GroupingState();
        var newGrouping = currentGrouping;

        if (newGrouping.Contains(Id))
            newGrouping = newGrouping.Remove(Id);
        else
            newGrouping = newGrouping.Add(Id);

        _table.SetState(state => state with { Grouping = newGrouping });
    }

    public void ToggleVisibility()
    {
        var currentVisibility = _table.State.ColumnVisibility ?? new ColumnVisibilityState();
        var newVisibility = currentVisibility.With(Id,
            !currentVisibility.GetValueOrDefault(Id, true));

        _table.SetState(state => state with { ColumnVisibility = newVisibility });
    }

    public void ResetSize()
    {
        var currentSizing = _table.State.ColumnSizing ?? new ColumnSizingState();
        var newSizing = currentSizing.Remove(Id);

        _table.SetState(state => state with { ColumnSizing = newSizing });
    }

    public void SetSize(double size)
    {
        var currentSizing = _table.State.ColumnSizing ?? new ColumnSizingState();
        var newSizing = currentSizing.With(Id, size);

        _table.SetState(state => state with { ColumnSizing = newSizing });
    }
}

public class Column<TData, TValue> : Column<TData>, IColumn<TData, TValue>
{
    public new ColumnDef<TData, TValue> ColumnDef { get; }
    public AccessorFn<TData, TValue>? AccessorFn { get; }

    public Column(Table<TData> table, ColumnDef<TData, TValue> columnDef, Column<TData>? parent, int depth)
        : base(table, columnDef, parent, depth)
    {
        ColumnDef = columnDef;
        AccessorFn = columnDef.AccessorFn ?? CreateAccessorFromKey(columnDef.AccessorKey);
    }

    private AccessorFn<TData, TValue>? CreateAccessorFromKey(string? accessorKey)
    {
        if (string.IsNullOrEmpty(accessorKey))
            return null;

        var parameter = Expression.Parameter(typeof(TData), "data");
        var property = Expression.PropertyOrField(parameter, accessorKey);
        var lambda = Expression.Lambda<AccessorFn<TData, TValue>>(property, parameter);
        return lambda.Compile();
    }

    public TValue GetValue(Row<TData> row)
    {
        if (AccessorFn == null)
            return default(TValue)!;

        return AccessorFn(row.Original);
    }
}

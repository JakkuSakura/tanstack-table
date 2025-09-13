using System.Collections.Concurrent;

namespace TanStack.Table.Core;

public class Table<TData> : ITable<TData>
{
    private readonly Dictionary<string, Column<TData>> _columnMap = new();
    private readonly Dictionary<string, Row<TData>> _rowMap = new();
    private readonly List<ITableFeature<TData>> _features = new();
    private TableState<TData> _state;

    public TableOptions<TData> Options { get; }
    public TableState<TData> State => _state;
    public IReadOnlyList<Column<TData>> AllColumns { get; private set; } = Array.Empty<Column<TData>>();
    public IReadOnlyList<Column<TData>> AllLeafColumns { get; private set; } = Array.Empty<Column<TData>>();
    public IReadOnlyList<Column<TData>> VisibleLeafColumns { get; private set; } = Array.Empty<Column<TData>>();
    public IReadOnlyList<HeaderGroup<TData>> HeaderGroups { get; private set; } = Array.Empty<HeaderGroup<TData>>();
    public IReadOnlyList<HeaderGroup<TData>> FooterGroups { get; private set; } = Array.Empty<HeaderGroup<TData>>();
    public RowModel<TData> RowModel { get; private set; }
    public RowModel<TData> PreFilteredRowModel { get; private set; }
    public RowModel<TData> PreSortedRowModel { get; private set; }
    public RowModel<TData> PreGroupedRowModel { get; private set; }
    public RowModel<TData> PreExpandedRowModel { get; private set; }
    public RowModel<TData> PrePaginationRowModel { get; private set; }

    public Table(TableOptions<TData> options)
    {
        Options = options;
        _state = options.State ?? new TableState<TData>();
        
        InitializeColumns();
        InitializeFeatures();
        UpdateRowModel();
    }

    private void InitializeColumns()
    {
        var allColumns = new List<Column<TData>>();
        var leafColumns = new List<Column<TData>>();
        
        BuildColumnTree(Options.Columns, null, 0, allColumns, leafColumns);
        
        AllColumns = allColumns.AsReadOnly();
        AllLeafColumns = leafColumns.AsReadOnly();
        
        foreach (var column in allColumns)
        {
            _columnMap[column.Id] = column;
        }
        
        UpdateVisibleColumns();
        UpdateHeaderGroups();
    }

    private void BuildColumnTree(
        IEnumerable<ColumnDef<TData>> columnDefs,
        Column<TData>? parent,
        int depth,
        List<Column<TData>> allColumns,
        List<Column<TData>> leafColumns)
    {
        foreach (var columnDef in columnDefs)
        {
            Column<TData> column;
            
            // 检测是否为泛型 ColumnDef<TData, TValue>
            var columnDefType = columnDef.GetType();
            if (columnDefType.IsGenericType &&
                columnDefType.GetGenericTypeDefinition() == typeof(ColumnDef<,>))
            {
                // 通过反射创建对应的 Column<TData, TValue>
                var valueType = columnDefType.GetGenericArguments()[1]; // TValue
                var columnType = typeof(Column<,>).MakeGenericType(typeof(TData), valueType);
                
                try
                {
                    column = (Column<TData>)Activator.CreateInstance(columnType, this, columnDef, parent, depth)!;
                }
                catch
                {
                    // 如果泛型创建失败，回退到基础 Column<TData>
                    column = new Column<TData>(this, columnDef, parent, depth);
                }
            }
            else
            {
                // 非泛型或 GroupColumnDef<TData> 等，使用基础 Column<TData>
                column = new Column<TData>(this, columnDef, parent, depth);
            }
            
            allColumns.Add(column);
            
            if (columnDef is GroupColumnDef<TData> groupDef)
            {
                BuildColumnTree(groupDef.Columns, column, depth + 1, allColumns, leafColumns);
            }
            else
            {
                leafColumns.Add(column);
            }
        }
    }

    private void InitializeFeatures()
    {
        if (Options.EnableColumnFilters)
            _features.Add(new ColumnFilteringFeature<TData>());
        if (Options.EnableGlobalFilter)
            _features.Add(new GlobalFilteringFeature<TData>());
        if (Options.EnableSorting)
            _features.Add(new SortingFeature<TData>());
        if (Options.EnableGrouping)
            _features.Add(new GroupingFeature<TData>());
        if (Options.EnableExpanding)
            _features.Add(new ExpandingFeature<TData>());
        if (Options.EnableRowSelection)
            _features.Add(new RowSelectionFeature<TData>());
        if (Options.EnablePagination)
            _features.Add(new PaginationFeature<TData>());

        foreach (var feature in _features)
        {
            feature.Initialize(this);
        }
    }

    private void UpdateRowModel()
    {
        var coreRowModel = GetCoreRowModel();
        PreFilteredRowModel = coreRowModel;
        
        var filteredRowModel = Options.GetFilteredRowModel?.Invoke(this) ?? coreRowModel;
        PreSortedRowModel = filteredRowModel;
        
        var sortedRowModel = Options.GetSortedRowModel?.Invoke(this) ?? filteredRowModel;
        PreGroupedRowModel = sortedRowModel;
        
        var groupedRowModel = Options.GetGroupedRowModel?.Invoke(this) ?? sortedRowModel;
        PreExpandedRowModel = groupedRowModel;
        
        var expandedRowModel = Options.GetExpandedRowModel?.Invoke(this) ?? groupedRowModel;
        PrePaginationRowModel = expandedRowModel;
        
        RowModel = Options.GetPaginationRowModel?.Invoke(this) ?? expandedRowModel;
        
        UpdateRowMap();
    }

    private RowModel<TData> GetCoreRowModel()
    {
        if (Options.GetCoreRowModel != null)
            return Options.GetCoreRowModel(Options.Data.ToArray());
            
        var rows = new List<Row<TData>>();
        var flatRows = new List<Row<TData>>();
        var rowsById = new Dictionary<string, Row<TData>>();
        
        var index = 0;
        foreach (var data in Options.Data)
        {
            var row = new Row<TData>(this, $"{index}", index, data, 0, null);
            rows.Add(row);
            flatRows.Add(row);
            rowsById[row.Id] = row;
            index++;
        }
        
        return new RowModel<TData>
        {
            Rows = rows.AsReadOnly(),
            FlatRows = flatRows.AsReadOnly(),
            RowsById = rowsById.AsReadOnly()
        };
    }

    private void UpdateRowMap()
    {
        _rowMap.Clear();
        foreach (var row in RowModel.FlatRows)
        {
            _rowMap[row.Id] = row;
        }
    }

    private void UpdateVisibleColumns()
    {
        var visibilityState = State.ColumnVisibility ?? new ColumnVisibilityState();

        VisibleLeafColumns = AllLeafColumns
            .Where(column => visibilityState.GetValueOrDefault(column.Id, true))
            .ToList()
            .AsReadOnly();
    }

    private void UpdateHeaderGroups()
    {
        var headerGroups = new List<HeaderGroup<TData>>();
        var footerGroups = new List<HeaderGroup<TData>>();
        
        var maxDepth = AllColumns.Any() ? AllColumns.Max(c => c.Depth) : 0;
        
        for (int depth = 0; depth <= maxDepth; depth++)
        {
            var headers = AllColumns
                .Where(c => c.Depth == depth && c.IsVisible)
                .Select(c => new Header<TData>(c, depth))
                .Cast<Header<TData>>()
                .ToList();
                
            if (headers.Any())
            {
                headerGroups.Add(new HeaderGroup<TData>($"headerGroup_{depth}", depth, headers));
                footerGroups.Insert(0, new HeaderGroup<TData>($"footerGroup_{depth}", depth, headers));
            }
        }
        
        HeaderGroups = headerGroups.AsReadOnly();
        FooterGroups = footerGroups.AsReadOnly();
    }

    public void SetState(TableState<TData> state)
    {
        var oldState = _state;
        _state = state;
        
        Options.OnStateChange?.Invoke(state);
        
        foreach (var feature in _features)
        {
            feature.OnStateChange(this, state);
        }
        
        if (!ReferenceEquals(oldState.ColumnVisibility, state.ColumnVisibility))
        {
            UpdateVisibleColumns();
            UpdateHeaderGroups();
        }
        
        UpdateRowModel();
    }

    public void SetState(Updater<TableState<TData>> updater)
    {
        SetState(updater(_state));
    }

    public Column<TData>? GetColumn(string columnId)
    {
        return _columnMap.GetValueOrDefault(columnId);
    }

    public Row<TData>? GetRow(string rowId)
    {
        return _rowMap.GetValueOrDefault(rowId);
    }

    public IReadOnlyList<Row<TData>> GetSelectedRowModel()
    {
        var selectionState = State.RowSelection ?? new RowSelectionState();
        return RowModel.FlatRows
            .Where(row => selectionState.GetValueOrDefault(row.Id, false))
            .ToList()
            .AsReadOnly();
    }

    public void ResetColumnFilters()
    {
        SetState(state => state with { ColumnFilters = null });
    }

    public void ResetGlobalFilter()
    {
        SetState(state => state with { GlobalFilter = null });
    }

    public void ResetSorting()
    {
        SetState(state => state with { Sorting = null });
    }

    public void ResetRowSelection()
    {
        SetState(state => state with { RowSelection = null });
    }

    public void ResetColumnOrder()
    {
        SetState(state => state with { ColumnOrder = null });
    }

    public void ResetColumnSizing()
    {
        SetState(state => state with { ColumnSizing = null });
    }

    public void ResetColumnVisibility()
    {
        SetState(state => state with { ColumnVisibility = null });
    }

    public void ResetExpanded()
    {
        SetState(state => state with { Expanded = null });
    }

    public void ResetGrouping()
    {
        SetState(state => state with { Grouping = null });
    }

    public void ResetPagination()
    {
        SetState(state => state with { Pagination = null });
    }
}
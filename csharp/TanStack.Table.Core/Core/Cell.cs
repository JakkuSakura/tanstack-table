namespace TanStack.Table.Core;

public class Cell<TData> : ICell<TData>
{
    public string Id { get; }
    public Column<TData> Column { get; }
    public Row<TData> Row { get; }
    public object? Value { get; }
    public object? RenderValue { get; }
    public bool IsGrouped { get; }
    public bool IsAggregated { get; }
    public bool IsPlaceholder { get; }

    IColumn<TData> ICell<TData>.Column => Column;
    IRow<TData> ICell<TData>.Row => Row;

    public Cell(Row<TData> row, Column<TData> column)
    {
        Row = row;
        Column = column;
        Id = $"{row.Id}_{column.Id}";
        
        Value = GetCellValue();
        RenderValue = Value; // TODO: Implement render value logic
        
        IsGrouped = row.IsGrouped && column.IsGrouped;
        IsAggregated = false; // TODO: Implement aggregation logic
        IsPlaceholder = false; // TODO: Implement placeholder logic
    }

    private object? GetCellValue()
    {
        // Try to get value using typed column accessor
        if (Column.ColumnDef is ColumnDef<TData, object> typedColumnDef)
        {
            var accessorFn = typedColumnDef.AccessorFn;
            if (accessorFn != null)
                return accessorFn(Row.Original);
        }
        
        // Fallback to reflection-based access
        if (Column.ColumnDef is ColumnDef<TData, object> columnDef && !string.IsNullOrEmpty(columnDef.AccessorKey))
        {
            var property = typeof(TData).GetProperty(columnDef.AccessorKey);
            if (property != null)
                return property.GetValue(Row.Original);
                
            var field = typeof(TData).GetField(columnDef.AccessorKey);
            if (field != null)
                return field.GetValue(Row.Original);
        }
        
        return null;
    }
}

public class Cell<TData, TValue> : Cell<TData>, ICell<TData, TValue>
{
    public new Column<TData, TValue> Column { get; }
    public new TValue Value { get; }
    public new TValue RenderValue { get; }

    IColumn<TData, TValue> ICell<TData, TValue>.Column => Column;

    public Cell(Row<TData> row, Column<TData, TValue> column) : base(row, column)
    {
        Column = column;
        Value = column.GetValue(row);
        RenderValue = Value; // TODO: Implement render value logic
    }
}
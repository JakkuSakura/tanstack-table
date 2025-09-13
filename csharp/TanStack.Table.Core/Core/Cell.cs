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
        var def = Column.ColumnDef;
        var defType = def.GetType();

        // 1. AccessorFn (任意 TValue) —— 形如 Func<TData, TValue>
        var accessorFnProp = defType.GetProperty("AccessorFn");
        if (accessorFnProp != null)
        {
            if (accessorFnProp.GetValue(def) is Delegate del)
            {
                try
                {
                    var v = del.DynamicInvoke(Row.Original);
                    if (v != null) return v;
                }
                catch { /* 吞掉单元格级别异常，继续尝试下一策略 */ }
            }
        }

        // 2. AccessorKey（如果列是通过 key 定义的）
        var accessorKeyProp = defType.GetProperty("AccessorKey");
        if (accessorKeyProp != null)
        {
            var key = accessorKeyProp.GetValue(def) as string;
            if (!string.IsNullOrWhiteSpace(key))
            {
                var dataType = typeof(TData);
                var prop = dataType.GetProperty(key);
                if (prop != null)
                    return prop.GetValue(Row.Original);

                var field = dataType.GetField(key);
                if (field != null)
                    return field.GetValue(Row.Original);
            }
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
        // 对于泛型列可直接使用列的 AccessorFn 以避免二次反射
        if (column.AccessorFn != null)
        {
            try
            {
                Value = column.AccessorFn(row.Original);
            }
            catch
            {
                Value = default!;
            }
        }
        else
        {
            // 退回基类已算好的 object? Value
            var baseVal = base.Value;
            Value = baseVal is TValue tv ? tv : default!;
        }
        RenderValue = Value;
    }
}

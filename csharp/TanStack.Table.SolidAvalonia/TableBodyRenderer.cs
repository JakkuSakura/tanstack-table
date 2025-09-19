using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using TanStack.Table.Core;

namespace TanStack.Table.SolidAvalonia;

internal class TableBodyRenderer<TData>
{
    private readonly TableCellRenderer<TData> _cellRenderer;

    public TableBodyRenderer()
    {
        _cellRenderer = new TableCellRenderer<TData>();
    }

    public Control CreateBody(Table<TData> table, Func<Table<TData>>? tableSignalGetter = null, Func<int>? selectionSignalGetter = null)
    {
        return new ScrollViewer()
            .Content(
                new StackPanel()
                    .Orientation(Orientation.Vertical)
                    .Children(
                        table.RowModel.Rows.Select(row =>
                            CreateRow(table, row, tableSignalGetter, selectionSignalGetter)
                        ).ToArray()
                    )
            );
    }

    private Control CreateRow(Table<TData> table, Row<TData> row, Func<Table<TData>>? tableSignalGetter = null, Func<int>? selectionSignalGetter = null)
    {
        Console.WriteLine($"DEBUG cell row={row.Index} col=id value={row.Original}");

        var cells = table.VisibleLeafColumns.Select(column =>
        {
            Console.WriteLine($"DEBUG cell row={row.Index} col={column.Id} value={TableContentHelper<TData>.GetCellContent(row, column)}");
            
            // Use reactive cells for SaGrid to support cell selection
            if (table is SaGrid<TData> saGrid && tableSignalGetter != null)
            {
                return _cellRenderer.CreateReactiveCell(saGrid, row, column, tableSignalGetter, selectionSignalGetter);
            }
            else
            {
                return _cellRenderer.CreateCell(table, row, column);
            }
        }).ToArray();

        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Children(cells);
    }
}
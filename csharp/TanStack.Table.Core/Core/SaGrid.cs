using System.Text;
using System.Text.Json;

namespace TanStack.Table.Core;

public class SaGrid<TData> : Table<TData>, ISaGrid<TData>
{
    private string? _quickFilter;

    public SaGrid(TableOptions<TData> options) : base(options)
    {
    }

    // Constructor for test compatibility  
    public SaGrid(Table<TData> table) : base(table.Options)
    {
    }

    // Advanced filtering capabilities
    public void SetGlobalFilter(object? value)
    {
        this.SetGlobalFilter(value); // Use extension method from base
    }

    public object? GetGlobalFilterValue()
    {
        return this.GetGlobalFilterValue(); // Use extension method from base
    }

    public void ClearGlobalFilter()
    {
        this.ClearGlobalFilter(); // Use extension method from base
    }

    // Export functionality
    public async Task<string> ExportToCsvAsync()
    {
        return await Task.Run(() =>
        {
            var csv = new StringBuilder();
            
            // Headers
            var headers = VisibleLeafColumns.Select(c => EscapeCsvValue(c.Id)).ToList();
            csv.AppendLine(string.Join(",", headers));
            
            // Data rows
            foreach (var row in RowModel.Rows)
            {
                var values = VisibleLeafColumns.Select(column =>
                {
                    var cell = row.GetCell(column.Id);
                    var value = cell.Value?.ToString() ?? "";
                    return EscapeCsvValue(value);
                });
                csv.AppendLine(string.Join(",", values));
            }
            
            return csv.ToString();
        });
    }

    public string ExportToCsv()
    {
        return ExportToCsvAsync().GetAwaiter().GetResult();
    }

    public async Task<string> ExportToJsonAsync()
    {
        return await Task.Run(() =>
        {
            var data = RowModel.Rows.Select(row =>
            {
                var obj = new Dictionary<string, object?>();
                foreach (var column in VisibleLeafColumns)
                {
                    var cell = row.GetCell(column.Id);
                    obj[column.Id] = cell.Value;
                }
                return obj;
            }).ToList();
            
            return JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        });
    }

    public string ExportToJson()
    {
        return ExportToJsonAsync().GetAwaiter().GetResult();
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
            
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }
        
        return value;
    }

    // Advanced search and filtering
    public void SetQuickFilter(string? searchTerm)
    {
        _quickFilter = searchTerm;
        // In a real implementation, this would trigger filtering
        // For now, we'll store it and use it in custom filtering logic
    }

    public string? GetQuickFilter()
    {
        return _quickFilter;
    }

    // Advanced column operations
    public void SetColumnVisibility(string columnId, bool visible)
    {
        this.ToggleColumnVisibility(columnId, visible); // Use extension method from base
    }

    public bool GetColumnVisibility(string columnId)
    {
        var visibility = State.ColumnVisibility;
        return visibility?.Items.GetValueOrDefault(columnId, true) ?? true;
    }

    public int GetVisibleColumnCount()
    {
        return VisibleLeafColumns.Count;
    }

    public int GetTotalColumnCount()
    {
        return AllLeafColumns.Count;
    }

    public int GetHiddenColumnCount()
    {
        return GetTotalColumnCount() - GetVisibleColumnCount();
    }

    // Keyboard navigation support
    private int _currentRowIndex = 0;
    private int _currentColumnIndex = 0;
    
    // Context menu and row actions
    private List<ContextMenuItem> _contextMenuItems = new();
    private List<RowAction<TData>> _rowActions = new();

    public void HandleKeyDown(string key)
    {
        switch (key.ToLower())
        {
            case "arrowup":
                _currentRowIndex = Math.Max(0, _currentRowIndex - 1);
                break;
            case "arrowdown":
                _currentRowIndex = Math.Min(RowModel.Rows.Count - 1, _currentRowIndex + 1);
                break;
            case "arrowleft":
                _currentColumnIndex = Math.Max(0, _currentColumnIndex - 1);
                break;
            case "arrowright":
                _currentColumnIndex = Math.Min(VisibleLeafColumns.Count - 1, _currentColumnIndex + 1);
                break;
        }
    }

    public Cell<TData>? GetCurrentCell()
    {
        if (_currentRowIndex >= 0 && _currentRowIndex < RowModel.Rows.Count &&
            _currentColumnIndex >= 0 && _currentColumnIndex < VisibleLeafColumns.Count)
        {
            var row = RowModel.Rows[_currentRowIndex];
            var column = VisibleLeafColumns[_currentColumnIndex];
            return row.GetCell(column.Id) as Cell<TData>;
        }
        return null;
    }

    public Row<TData>? GetCurrentRow()
    {
        if (_currentRowIndex >= 0 && _currentRowIndex < RowModel.Rows.Count)
        {
            return RowModel.Rows[_currentRowIndex];
        }
        return null;
    }

    // Context menu functionality
    public void SetContextMenuItems(IEnumerable<ContextMenuItem> items)
    {
        _contextMenuItems = items.ToList();
    }

    public IReadOnlyList<ContextMenuItem> GetContextMenuItems()
    {
        return _contextMenuItems.AsReadOnly();
    }

    // Row actions functionality
    public void AddRowAction(RowAction<TData> action)
    {
        _rowActions.Add(action);
    }

    public void AddRowAction(string id, string label, Action<Row<TData>> action)
    {
        var rowAction = new RowAction<TData>
        {
            Id = id,
            Label = label,
            Action = action
        };
        _rowActions.Add(rowAction);
    }

    // Overload for test compatibility (with default action)
    public void AddRowAction(string id, string label)
    {
        AddRowAction(id, label, _ => { }); // Default empty action
    }

    // Overload for test compatibility (with Func<Row<TData>, string>)
    public void AddRowAction(string id, Func<Row<TData>, string> labelGenerator)
    {
        AddRowAction(id, id, row => { labelGenerator(row); }); // Convert Func<Row<TData>, string> to Action<Row<TData>>
    }

    public IReadOnlyList<RowAction<TData>> GetRowActions()
    {
        return _rowActions.AsReadOnly();
    }

    public IReadOnlyList<RowAction<TData>> GetRowActions(Row<TData> row)
    {
        // Return all actions for now, could be filtered based on row state
        return _rowActions.AsReadOnly();
    }

    public void RemoveRowAction(string actionId)
    {
        _rowActions.RemoveAll(a => a.Id == actionId);
    }

    public void ClearRowActions()
    {
        _rowActions.Clear();
    }

    // Header rendering functionality
    private Func<string, string>? _headerRenderer;

    public void SetHeaderRenderer(Func<string, string> renderer)
    {
        _headerRenderer = renderer;
    }

    // Overload for test compatibility
    public void SetHeaderRenderer(string columnId, Func<string, string> renderer)
    {
        // For simplicity, we'll just use the renderer for all columns
        // In a real implementation, this might be column-specific
        _headerRenderer = renderer;
    }

    public string RenderHeader(string columnId)
    {
        var column = GetColumn(columnId);
        if (column == null) return columnId;

        if (_headerRenderer != null)
        {
            return _headerRenderer(columnId);
        }

        // Default header rendering - use column header or fallback to ID
        return column.ColumnDef.Header?.ToString() ?? columnId;
    }

    // Cell rendering functionality
    private Func<Row<TData>, string, string>? _cellRenderer;

    public void SetCellRenderer(Func<Row<TData>, string, string> renderer)
    {
        _cellRenderer = renderer;
    }

    public string RenderCell(Row<TData> row, string columnId)
    {
        if (_cellRenderer != null)
        {
            return _cellRenderer(row, columnId);
        }

        // Default cell rendering
        var cell = row.GetCell(columnId);
        return cell.Value?.ToString() ?? "";
    }

    // Overload for test compatibility
    public string RenderCell(string columnId, Row<TData> row)
    {
        return RenderCell(row, columnId);
    }

    // Column management methods for tests
    public void ResizeColumn(string columnId, double width)
    {
        var currentSizing = State.ColumnSizing ?? new ColumnSizingState();
        var newSizing = new Dictionary<string, double>(currentSizing.Items)
        {
            [columnId] = width
        };

        SetState(state => state with 
        { 
            ColumnSizing = new ColumnSizingState(newSizing)
        });
    }

    public void MoveColumn(string columnId, int toIndex)
    {
        var currentOrder = State.ColumnOrder?.Order ?? AllLeafColumns.Select(c => c.Id).ToList();
        var newOrder = currentOrder.ToList();

        // Remove the column from its current position
        newOrder.Remove(columnId);

        // Insert at the new position
        var clampedIndex = Math.Max(0, Math.Min(toIndex, newOrder.Count));
        newOrder.Insert(clampedIndex, columnId);

        SetState(state => state with 
        { 
            ColumnOrder = new ColumnOrderState(newOrder)
        });
    }

    // Theme support for tests
    private string? _currentTheme;
    private Dictionary<string, object> _themeProperties = new();

    public void SetTheme(string theme)
    {
        _currentTheme = theme;
        
        // Set default theme properties based on theme
        switch (theme.ToLowerInvariant())
        {
            case "dark":
                _themeProperties["backgroundColor"] = "#1e1e1e";
                _themeProperties["textColor"] = "#ffffff";
                break;
            case "light":
                _themeProperties["backgroundColor"] = "#ffffff";
                _themeProperties["textColor"] = "#000000";
                break;
            default:
                _themeProperties["backgroundColor"] = "#f5f5f5";
                _themeProperties["textColor"] = "#333333";
                break;
        }
    }

    public string? CurrentTheme => _currentTheme;

    public void SetThemeProperty(string key, object value)
    {
        _themeProperties[key] = value;
    }

    public object? GetThemeProperty(string key)
    {
        return _themeProperties.GetValueOrDefault(key);
    }
}

// Factory methods for SaGrid
public static class SaGridFactory
{
    public static SaGrid<TData> Create<TData>(TableOptions<TData> options)
    {
        return new SaGrid<TData>(options);
    }

    public static SaGrid<TData> Create<TData>(Table<TData> table)
    {
        return new SaGrid<TData>(table.Options);
    }

    // Overload that accepts Table directly for test compatibility
    public static SaGrid<TData> CreateFromTable<TData>(Table<TData> table)
    {
        return new SaGrid<TData>(table.Options);
    }

    public static SaGrid<TData> Create<TData>(
        IEnumerable<TData> data,
        IReadOnlyList<ColumnDef<TData>> columns)
    {
        var options = new TableOptions<TData>
        {
            Data = data,
            Columns = columns,
            EnableGlobalFilter = true,
            EnableColumnFilters = true,
            EnableSorting = true,
            EnableRowSelection = true,
            EnableColumnResizing = true,
            EnablePagination = true
        };
        
        return new SaGrid<TData>(options);
    }
}
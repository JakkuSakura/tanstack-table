using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using TanStack.Table.Core;
using SaGrid;
using SolidAvalonia;
using static SolidAvalonia.Solid;
using System.Diagnostics;

namespace Examples;

public record Person(int Id, string FirstName, string LastName, int Age, string Email, string Department, bool IsActive);


public class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private SaGrid<Person> saGrid = null!;
    private TextBlock infoTextBlock = null!;

    private void InitializeComponent()
    {
        // Window properties
        Title = "SaGrid Advanced Table - Full Featured Demo";
        Width = 1400;
        Height = 900;

        // Generate larger sample data to showcase advanced features
        var people = GenerateLargeDataset(100);

        // Define advanced columns with sorting and filtering capabilities
        var columns = new List<ColumnDef<Person>>
        {
            ColumnHelper.Accessor<Person, int>(accessorFn: p => p.Id, id: "id", header: "ID"),
            ColumnHelper.Accessor<Person, string>(accessorFn: p => p.FirstName, id: "firstName", header: "First Name"),
            ColumnHelper.Accessor<Person, string>(accessorFn: p => p.LastName, id: "lastName", header: "Last Name"),
            ColumnHelper.Accessor<Person, int>(accessorFn: p => p.Age, id: "age", header: "Age"),
            ColumnHelper.Accessor<Person, string>(accessorFn: p => p.Email, id: "email", header: "Email"),
            ColumnHelper.Accessor<Person, string>(accessorFn: p => p.Department, id: "department", header: "Department"),
            ColumnHelper.Accessor<Person, bool>(accessorFn: p => p.IsActive, id: "isActive", header: "Active")
        };

        // Create SaGrid with advanced features enabled
        var options = new TableOptions<Person>
        {
            Data = people,
            Columns = columns.AsReadOnly(),
            EnableSorting = true,
            EnableGlobalFilter = true,
            EnableColumnFilters = true,
            EnablePagination = true,
            EnableRowSelection = true,
            EnableCellSelection = true,
            EnableColumnResizing = true,
            State = new TableState<Person>
            {
                Pagination = new PaginationState { PageIndex = 0, PageSize = 10 }
            }
        };

        saGrid = new SaGrid<Person>(options);

        // Configure SaGrid advanced features
        ConfigureSaGridFeatures();
        
        // Start with no programmatic filters; user can filter via headers

        // Create UI with advanced SaGrid features
        var ui = CreateAdvancedUI();
        Content = ui;
        
        // Update info display
        UpdateInfoText();
    }

    private IEnumerable<Person> GenerateLargeDataset(int count)
    {
        var random = new Random(42);
        var departments = new[] { "Engineering", "Marketing", "Sales", "HR", "Finance", "Operations", "Support" };
        var firstNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Jane", "John" };
        var lastNames = new[] { "Anderson", "Brown", "Davis", "Garcia", "Johnson", "Jones", "Miller", "Smith", "Taylor", "Williams" };

        for (int i = 1; i <= count; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            yield return new Person(
                i,
                $"{firstName}{i}",
                $"{lastName}{i}",
                random.Next(22, 65),
                $"{firstName.ToLower()}.{lastName.ToLower()}{i}@example.com",
                departments[random.Next(departments.Length)],
                random.Next(0, 10) < 8 // 80% active
            );
        }
    }

    private void ConfigureSaGridFeatures()
    {
        // No theming toggle in example; focus on table features
        
        // Add row actions
        saGrid.AddRowAction("edit", "Edit", row => 
        {
            Debug.WriteLine($"Edit clicked for {row.Original.FirstName} {row.Original.LastName}");
        });
        
        saGrid.AddRowAction("delete", "Delete", row => 
        {
            Debug.WriteLine($"Delete clicked for {row.Original.FirstName} {row.Original.LastName}");
        });

        // Set up custom header renderers for some columns
        saGrid.SetHeaderRenderer("age", columnId => $"📅 Age");
        saGrid.SetHeaderRenderer("department", columnId => $"🏢 Dept");
        saGrid.SetHeaderRenderer("isActive", columnId => $"✅ Status");

        // Set up custom cell renderers
        saGrid.SetCellRenderer((row, columnId) =>
        {
            return columnId switch
            {
                "isActive" => row.Original.IsActive ? "✅ Active" : "❌ Inactive",
                "age" => $"{row.Original.Age} years",
                "department" => $"[{row.Original.Department}]",
                _ => row.GetCell(columnId).Value?.ToString() ?? ""
            };
        });
    }

    private Control CreateAdvancedUI()
    {
        var container = new StackPanel { Orientation = Orientation.Vertical };

        // Simple header
        var header = new TextBlock
        {
            Text = "SaGrid Advanced Table Demo",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(20, 10),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        container.Children.Add(header);

        // Removed test TextBox used during debugging

        // Controls section
        var controlsPanel = CreateControlsPanel();
        container.Children.Add(controlsPanel);

        // Info panel
        infoTextBlock = new TextBlock
        {
            Text = "SaGrid Information: Initializing...",
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(20, 10),
            FontSize = 14
        };
        container.Children.Add(infoTextBlock);

        // Create SaGridComponent display for SaGrid
        var saGridComponent = new SaGridComponent<Person>(saGrid);
        container.Children.Add(saGridComponent);

        return new ScrollViewer { Content = container };
    }

    private Control CreateControlsPanel()
    {
        var panel = new StackPanel 
        { 
            Orientation = Orientation.Vertical,
            Margin = new Thickness(20, 10)
        };

        // Minimal, reliable actions
        var buttonPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var multiSortBtn = new Button 
        { 
            Content = "⇅ Toggle Multi‑Sort", 
            Padding = new Thickness(10, 5)
        };
        multiSortBtn.Click += (sender, e) =>
        {
            saGrid.ToggleMultiSortOverride();
            UpdateInfoText();
        };

        var resetFiltersBtn = new Button 
        { 
            Content = "🧹 Reset Filters", 
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(10, 5)
        };
        resetFiltersBtn.Click += (sender, e) =>
        {
            saGrid.ClearGlobalFilter();
            saGrid.ClearColumnFilters();
            UpdateInfoText();
        };

        var resetSortingBtn = new Button 
        { 
            Content = "↕️ Reset Sorting", 
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(10, 5)
        };
        resetSortingBtn.Click += (sender, e) =>
        {
            saGrid.SetSorting(Array.Empty<ColumnSort>());
            UpdateInfoText();
        };

        buttonPanel.Children.Add(multiSortBtn);
        buttonPanel.Children.Add(resetFiltersBtn);
        buttonPanel.Children.Add(resetSortingBtn);
        panel.Children.Add(buttonPanel);

        return panel;
    }

    private void UpdateInfoText()
    {
        if (infoTextBlock != null && saGrid != null)
        {
            var visibleRows = saGrid.RowModel.Rows.Count;
            var totalColumns = saGrid.AllLeafColumns.Count;
            var visibleColumns = saGrid.VisibleLeafColumns.Count;
            var hasGlobalFilter = saGrid.State.GlobalFilter != null;
            var hasColumnFilters = saGrid.State.ColumnFilters?.Filters.Count > 0;
            var multiSort = saGrid.IsMultiSortEnabled() ? "ON" : "OFF";
            
            // Cell selection info
            var selectedCells = saGrid.GetSelectedCells();
            var activeCell = saGrid.GetActiveCell();
            var cellSelectionInfo = selectedCells.Count > 0 
                ? $"Selected: {selectedCells.Count} cells" 
                : "No selection";
            
            if (activeCell != null)
            {
                cellSelectionInfo += $" | Active: ({activeCell.RowIndex},{activeCell.ColumnId})";
            }
            
            infoTextBlock.Text = $"📊 SaGrid Stats: {visibleRows} rows | {visibleColumns}/{totalColumns} columns | " +
                               $"Multi‑Sort: {multiSort} | Global Filter: {(hasGlobalFilter ? "✅" : "❌")} | " +
                               $"Column Filters: {(hasColumnFilters == true ? "✅" : "❌")} | " +
                               $"🎯 {cellSelectionInfo}";
        }
    }
}

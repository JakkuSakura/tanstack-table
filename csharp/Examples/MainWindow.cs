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
        
        // Test programmatic filtering
        Console.WriteLine("Testing programmatic filter - setting department filter to 'Engineering'");
        saGrid.SetColumnFilter("department", "Engineering");

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
        // Set up theming
        saGrid.SetTheme("light");
        
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

        // Test TextBox to verify input works outside the table
        var testTextBox = new TextBox
        {
            Watermark = "Test TextBox - type here...",
            Margin = new Thickness(20, 10),
            Width = 200
        };
        testTextBox.TextChanged += (s, e) => Debug.WriteLine($"Test TextBox changed: {testTextBox.Text}");
        container.Children.Add(testTextBox);

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

        // Note: Column filtering is now handled directly in the table headers

        // Action buttons
        var buttonPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var exportCsvBtn = new Button 
        { 
            Content = "📄 Export CSV", 
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(10, 5)
        };
        exportCsvBtn.Click += (sender, e) =>
        {
            try
            {
                var csv = saGrid.ExportToCsv();
                Debug.WriteLine($"CSV Export: {csv.Length} characters");
                Debug.WriteLine("CSV Content Preview:");
                Debug.WriteLine(csv.Substring(0, Math.Min(200, csv.Length)) + "...");
                
                // Update UI to show export happened
                infoTextBlock.Text += " | CSV Export: ✅ Success";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CSV Export Error: {ex.Message}");
                infoTextBlock.Text += " | CSV Export: ❌ Error";
            }
        };

        var exportJsonBtn = new Button 
        { 
            Content = "📋 Export JSON", 
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(10, 5)
        };
        exportJsonBtn.Click += (sender, e) =>
        {
            try
            {
                var json = saGrid.ExportToJson();
                Debug.WriteLine($"JSON Export: {json.Length} characters");
                Debug.WriteLine("JSON Content Preview:");
                Debug.WriteLine(json.Substring(0, Math.Min(200, json.Length)) + "...");
                
                // Update UI to show export happened
                infoTextBlock.Text += " | JSON Export: ✅ Success";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"JSON Export Error: {ex.Message}");
                infoTextBlock.Text += " | JSON Export: ❌ Error";
            }
        };

        var themeBtn = new Button 
        { 
            Content = "🎨 Toggle Theme", 
            Padding = new Thickness(10, 5)
        };
        themeBtn.Click += (sender, e) =>
        {
            var currentTheme = saGrid.CurrentTheme ?? "light";
            var newTheme = currentTheme == "light" ? "dark" : "light";
            saGrid.SetTheme(newTheme);
            UpdateInfoText();
        };

        buttonPanel.Children.Add(exportCsvBtn);
        buttonPanel.Children.Add(exportJsonBtn);
        buttonPanel.Children.Add(themeBtn);
        panel.Children.Add(buttonPanel);

        // Cell selection controls
        var cellSelectionPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var selectCellBtn = new Button 
        { 
            Content = "🎯 Select Cell (0,1)", 
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(10, 5)
        };
        selectCellBtn.Click += (sender, e) =>
        {
            saGrid.SelectCell(0, "firstName");
            UpdateInfoText();
        };

        var selectRangeBtn = new Button 
        { 
            Content = "📐 Select Range (0,0)-(2,2)", 
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(10, 5)
        };
        selectRangeBtn.Click += (sender, e) =>
        {
            saGrid.SelectCellRange(0, "id", 2, "age");
            UpdateInfoText();
        };

        var copyCellsBtn = new Button 
        { 
            Content = "📋 Copy Selected", 
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(10, 5)
        };
        copyCellsBtn.Click += (sender, e) =>
        {
            try
            {
                var copiedText = saGrid.CopySelectedCells();
                if (!string.IsNullOrEmpty(copiedText))
                {
                    Debug.WriteLine("Copied to clipboard:");
                    Debug.WriteLine(copiedText);
                    infoTextBlock.Text += " | Copied: ✅ Success";
                }
                else
                {
                    infoTextBlock.Text += " | Copied: ❌ No selection";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Copy error: {ex.Message}");
                infoTextBlock.Text += " | Copied: ❌ Error";
            }
        };

        var clearSelectionBtn = new Button 
        { 
            Content = "🗑️ Clear Selection", 
            Padding = new Thickness(10, 5)
        };
        clearSelectionBtn.Click += (sender, e) =>
        {
            saGrid.ClearCellSelection();
            UpdateInfoText();
        };

        cellSelectionPanel.Children.Add(selectCellBtn);
        cellSelectionPanel.Children.Add(selectRangeBtn);
        cellSelectionPanel.Children.Add(copyCellsBtn);
        cellSelectionPanel.Children.Add(clearSelectionBtn);
        panel.Children.Add(cellSelectionPanel);

        return panel;
    }

    private void UpdateInfoText()
    {
        if (infoTextBlock != null && saGrid != null)
        {
            var theme = saGrid.CurrentTheme ?? "default";
            var visibleRows = saGrid.RowModel.Rows.Count;
            var totalColumns = saGrid.AllLeafColumns.Count;
            var visibleColumns = saGrid.VisibleLeafColumns.Count;
            var hasGlobalFilter = saGrid.State.GlobalFilter != null;
            var hasColumnFilters = saGrid.State.ColumnFilters?.Filters.Count > 0;
            
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
                               $"Theme: {theme} | Global Filter: {(hasGlobalFilter ? "✅" : "❌")} | " +
                               $"Column Filters: {(hasColumnFilters == true ? "✅" : "❌")} | " +
                               $"🎯 {cellSelectionInfo}";
        }
    }
}

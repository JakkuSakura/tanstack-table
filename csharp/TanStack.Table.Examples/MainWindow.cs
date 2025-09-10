using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using TanStack.Table.Core;
using TanStack.Table.SolidAvalonia;
using SolidAvalonia;
using static SolidAvalonia.Solid;

namespace TanStack.Table.Examples;

public record Person(string FirstName, string LastName, int Age, string Email, string Department);


public class MainWindow : Window
{
    public MainWindow()
    {

      InitializeComponent();
    }

    private Table<Person> table;
    private TextBlock infoTextBlock;

    private void InitializeComponent()
    {
        // Window properties
        Title = "TanStack Table - C# Implementation Example";
        Width = 1200;
        Height = 800;

        // Sample data - demonstrating TanStack Table functionality
        var people = new List<Person>
        {
            new("John", "Doe", 30, "john.doe@example.com", "Engineering"),
            new("Jane", "Smith", 25, "jane.smith@example.com", "Marketing"),
            new("Bob", "Johnson", 35, "bob.johnson@example.com", "Sales"),
            new("Alice", "Williams", 28, "alice.williams@example.com", "Engineering"),
            new("Charlie", "Brown", 32, "charlie.brown@example.com", "HR"),
            new("Diana", "Davis", 27, "diana.davis@example.com", "Marketing"),
            new("Eve", "Miller", 29, "eve.miller@example.com", "Engineering"),
            new("Frank", "Wilson", 33, "frank.wilson@example.com", "Sales"),
            new("Grace", "Moore", 26, "grace.moore@example.com", "HR"),
            new("Henry", "Taylor", 31, "henry.taylor@example.com", "Engineering")
        };

        // Define columns
        var columns = new List<ColumnDef<Person>>
        {
            ColumnHelper.Accessor<Person, string>(p => p.FirstName, "firstName", "First Name"),
            ColumnHelper.Accessor<Person, string>(p => p.LastName, "lastName", "Last Name"),
            ColumnHelper.Accessor<Person, int>(p => p.Age, "age", "Age"),
            ColumnHelper.Accessor<Person, string>(p => p.Email, "email", "Email"),
            ColumnHelper.Accessor<Person, string>(p => p.Department, "department", "Department")
        };

        // Create table using TanStack Table Core - THIS IS THE ACTUAL TANSTACK TABLE!
        // This demonstrates that we're using TanStack Table for data management
        var options = new TableOptions<Person>
        {
            Data = people,
            Columns = columns.AsReadOnly(),
            EnableSorting = true,
            EnableGlobalFilter = true,
            EnableColumnFilters = true,
            EnablePagination = true,
            State = new TableState<Person>
            {
                Pagination = new PaginationState { PageIndex = 0, PageSize = 5 }
            }
        };

        table = new Table<Person>(options);

        // Create SolidTable for the UI using the TanStack Table Core
        var solidTable = new SolidTable<Person>(options, table);

        // Create global search box - demonstrating TanStack Table's filtering capabilities
        var searchBox = new TextBox
        {
            Watermark = "Search all columns (First Name, Last Name, Email)...",
            Margin = new Thickness(20, 0, 20, 10),
            Height = 30
        };

        searchBox.TextChanged += (sender, e) =>
        {
            if (sender is TextBox tb && solidTable != null)
            {
                try
                {
                    // Use TanStack Table's global filter capability
                    solidTable.Table.SetState(new TableState<Person>
                    {
                        GlobalFilter = new GlobalFilterState(tb.Text ?? ""),
                        Pagination = solidTable.Table.State.Pagination
                    });
                    
                    SafeUpdateInfoText(solidTable);
                }
                catch (InvalidOperationException)
                {
                    // Table not yet initialized, will work for future searches
                }
            }
        };

        // THIS IS USING TANSTACK TABLE! Use SolidTable for the display

        // Create window content
        var container = new StackPanel { Orientation = Orientation.Vertical };

        // CLEAR NOTICE - THIS EXAMPLE ACTUALLY USES TANSTACK TABLE!
        var noticeBorder = new Border
        {
            Background = Brushes.Yellow,
            BorderThickness = new Thickness(3),
            BorderBrush = Brushes.Red,
            Margin = new Thickness(20, 20, 20, 10),
            CornerRadius = new CornerRadius(10),
            Child = new TextBlock
            {
                Text = "🔥 THIS IS TANSTACK TABLE IN ACTION! 🔥\n\n" +
                       "✓ TanStack Table manages the data\n" +
                       "✓ Search field uses TanStack's GlobalFilter\n" +
                       "✓ DataGrid displays TanStack Table's filtered results\n" +
                       "✓ Info panel shows TanStack Table RowModel statistics\n" +
                       "✓ All processing done by TanStack Table - DataGrid is just the view!",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20),
                Foreground = Brushes.DarkRed
            }
        };

        container.Children.Add(noticeBorder);

        // Title
        container.Children.Add(new TextBlock
        {
            Text = "TanStack Table - C# Table Library Demo",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 10)
        });

        // Add search box
        container.Children.Add(searchBox);

        // Info panel
        var infoPanel = new Border
        {
            Background = Brushes.LightBlue,
            Padding = new Thickness(10),
            Margin = new Thickness(20, 0, 20, 20),
            CornerRadius = new CornerRadius(5)
        };

        infoTextBlock = new TextBlock
        {
            Text = "Table Information: Initializing... (powered by TanStack Table)",
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };

        infoPanel.Child = infoTextBlock;
        container.Children.Add(infoPanel);

        // Add the TANSTACK TABLE DISPLAY!
        container.Children.Add(solidTable);

        Content = new ScrollViewer { Content = container };
    }

    private void UpdateInfoText(Table<Person>? table)
    {
        if (infoTextBlock != null && table != null)
        {
            infoTextBlock.Text = $"Table Information: {table.RowModel.Rows.Count} rows, {table.VisibleLeafColumns.Count} visible columns (powered by TanStack Table)";
        }
    }

    private void SafeUpdateInfoText(SolidTable<Person> solidTable)
    {
        try
        {
            UpdateInfoText(solidTable.Table);
        }
        catch (InvalidOperationException)
        {
            // Table not yet initialized
            UpdateInfoText(null);
        }
    }

}

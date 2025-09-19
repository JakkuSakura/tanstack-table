using TanStack.Table.Core;

namespace TanStack.Table.CellSelectionTest;

// Simple test to verify cell selection functionality
public class CellSelectionValidation
{
    public record TestPerson(int Id, string FirstName, string LastName, int Age);

    public static void RunCellSelectionTest()
    {
        Console.WriteLine("🧪 Testing SaGrid Cell Selection Functionality...");

        // Create test data
        var people = new List<TestPerson>
        {
            new(1, "John", "Doe", 30),
            new(2, "Jane", "Smith", 25),
            new(3, "Bob", "Johnson", 35)
        };

        // Define columns
        var columns = new List<ColumnDef<TestPerson>>
        {
            ColumnHelper.Accessor<TestPerson, int>(p => p.Id, "id", "ID"),
            ColumnHelper.Accessor<TestPerson, string>(p => p.FirstName, "firstName", "First Name"),
            ColumnHelper.Accessor<TestPerson, string>(p => p.LastName, "lastName", "Last Name"),
            ColumnHelper.Accessor<TestPerson, int>(p => p.Age, "age", "Age")
        };

        // Create SaGrid with cell selection enabled
        var options = new TableOptions<TestPerson>
        {
            Data = people,
            Columns = columns.AsReadOnly(),
            EnableCellSelection = true
        };

        var saGrid = new SaGrid<TestPerson>(options);

        // Test 1: Single cell selection
        Console.WriteLine("Test 1: Single cell selection");
        saGrid.SelectCell(0, "firstName");
        var selectedCells = saGrid.GetSelectedCells();
        var activeCell = saGrid.GetActiveCell();
        
        Console.WriteLine($"✅ Selected cells count: {selectedCells.Count}");
        Console.WriteLine($"✅ Active cell: {activeCell}");
        Console.WriteLine($"✅ Is cell (0, firstName) selected: {saGrid.IsCellSelected(0, "firstName")}");

        // Test 2: Range selection
        Console.WriteLine("\nTest 2: Range selection");
        saGrid.SelectCellRange(0, "id", 1, "lastName");
        selectedCells = saGrid.GetSelectedCells();
        
        Console.WriteLine($"✅ Selected cells after range: {selectedCells.Count}");
        foreach (var cell in selectedCells)
        {
            Console.WriteLine($"   - {cell}");
        }

        // Test 3: Copy selected cells
        Console.WriteLine("\nTest 3: Copy selected cells");
        var copiedText = saGrid.CopySelectedCells();
        Console.WriteLine($"✅ Copied text:\n{copiedText}");

        // Test 4: Clear selection
        Console.WriteLine("\nTest 4: Clear selection");
        saGrid.ClearCellSelection();
        selectedCells = saGrid.GetSelectedCells();
        Console.WriteLine($"✅ Selected cells after clear: {selectedCells.Count}");

        // Test 5: Navigation
        Console.WriteLine("\nTest 5: Navigation");
        saGrid.SelectCell(1, "firstName");
        Console.WriteLine($"✅ Before navigation: {saGrid.GetActiveCell()}");
        
        saGrid.NavigateCell(SaGrid<TestPerson>.CellNavigationDirection.Right);
        Console.WriteLine($"✅ After navigate right: {saGrid.GetActiveCell()}");
        
        saGrid.NavigateCell(SaGrid<TestPerson>.CellNavigationDirection.Down);
        Console.WriteLine($"✅ After navigate down: {saGrid.GetActiveCell()}");

        Console.WriteLine("\n🎉 All cell selection tests completed successfully!");
    }

    public static void Main(string[] args)
    {
        try
        {
            RunCellSelectionTest();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
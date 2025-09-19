using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Declarative;
using SolidAvalonia;
using TanStack.Table.Core;
using static SolidAvalonia.Solid;
using Avalonia;

namespace SaGrid;

public class SaGridComponent<TData> : Component
{
    private readonly SaGrid<TData> _saGrid;
    private (Func<SaGrid<TData>>, Action<SaGrid<TData>>)? _gridSignal;
    private (Func<int>, Action<int>)? _selectionSignal;

    // Renderers
    private readonly SaGridHeaderRenderer<TData> _headerRenderer;
    private readonly SaGridBodyRenderer<TData> _bodyRenderer;
    private readonly SaGridFooterRenderer<TData> _footerRenderer;

    public SaGrid<TData> Grid => _gridSignal?.Item1() ?? throw new InvalidOperationException("Grid not initialized. Call Build() first.");

    public SaGridComponent(SaGrid<TData> saGrid) : base(true)
    {
        _saGrid = saGrid;
        
        // Initialize renderers
        _headerRenderer = new SaGridHeaderRenderer<TData>();
        _bodyRenderer = new SaGridBodyRenderer<TData>();
        _footerRenderer = new SaGridFooterRenderer<TData>();
        
        OnCreatedCore(); // 推送 reactive owner
        Initialize(); // 触发 Build()
    }

    protected override object Build()
    {
        Console.WriteLine("SaGridComponent.Build invoked");
        
        // Initialize signals
        if (_gridSignal == null)
        {
            _gridSignal = CreateSignal(_saGrid);
            _selectionSignal = CreateSignal(0); // Signal for cell selection count

            var (gridGetter, gridSetter) = _gridSignal.Value;
            var (selectionGetter, selectionSetter) = _selectionSignal.Value;
            var updateCounter = 0;

            // Set up UI update callback for SaGrid
            _saGrid.SetUIUpdateCallback(() =>
            {
                updateCounter++;
                gridSetter?.Invoke(_saGrid);
                selectionSetter?.Invoke(updateCounter); // Use counter to force change
            });
        }

        return Reactive(() =>
        {
            var currentGrid = Grid; // Access the reactive signal for grid updates

            var mainBorder = new Border()
                .BorderThickness(1)
                .BorderBrush(Brushes.Gray)
                .Child(
                    new StackPanel()
                        .Children(
                            // Header (reactive to ensure TextBox events work)
                            _headerRenderer.CreateHeader(currentGrid),
                            // Body (reactive, updates with grid changes)
                            _bodyRenderer.CreateBody(currentGrid, () => Grid, _selectionSignal?.Item1),
                            // Footer (reactive)
                            _footerRenderer.CreateFooter(currentGrid)
                        )
                );

            // Add keyboard navigation for cell selection
            mainBorder.KeyDown += (sender, e) =>
            {
                try
                {
                    var direction = e.Key switch
                    {
                        Avalonia.Input.Key.Up => SaGrid<TData>.CellNavigationDirection.Up,
                        Avalonia.Input.Key.Down => SaGrid<TData>.CellNavigationDirection.Down,
                        Avalonia.Input.Key.Left => SaGrid<TData>.CellNavigationDirection.Left,
                        Avalonia.Input.Key.Right => SaGrid<TData>.CellNavigationDirection.Right,
                        _ => (SaGrid<TData>.CellNavigationDirection?)null
                    };

                    if (direction.HasValue)
                    {
                        Console.WriteLine($"Keyboard navigation: {direction.Value}");
                        currentGrid.NavigateCell(direction.Value);
                        e.Handled = true;
                    }
                    else if (e.Key == Avalonia.Input.Key.C && e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control))
                    {
                        // Ctrl+C to copy selected cells
                        var copiedText = currentGrid.CopySelectedCells();
                        if (!string.IsNullOrEmpty(copiedText))
                        {
                            Console.WriteLine($"Copied to clipboard:\n{copiedText}");
                        }
                        e.Handled = true;
                    }
                    else if (e.Key == Avalonia.Input.Key.Escape)
                    {
                        // Escape to clear selection
                        currentGrid.ClearCellSelection();
                        e.Handled = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling keyboard navigation: {ex.Message}");
                }
            };

            // Focus the border when clicked to enable keyboard navigation, but not if clicking on a TextBox
            mainBorder.PointerPressed += (sender, e) =>
            {
                // Don't steal focus if the user clicked on a TextBox or its container
                if (e.Source is not TextBox && e.Source is not Border border)
                {
                    mainBorder.Focus();
                }
                else if (e.Source is Border borderSource && borderSource.Child is TextBox)
                {
                    // If clicking on a border containing a TextBox, focus the TextBox instead
                    borderSource.Child.Focus();
                }
            };

            return mainBorder;
        });
    }
}
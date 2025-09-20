using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Declarative;
using SolidAvalonia;
using TanStack.Table.Core;
using static SolidAvalonia.Solid;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

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
        
        // Initialize signals and hook SaGrid UI update callback once
        if (_gridSignal == null)
        {
            _gridSignal = CreateSignal(_saGrid);
            _selectionSignal = CreateSignal(0);

            var (gridGetter, gridSetter) = _gridSignal.Value;
            var (selectionGetter, selectionSetter) = _selectionSignal.Value;
            var updateCounter = 0;

            _saGrid.SetUIUpdateCallback(() =>
            {
                updateCounter++;
                gridSetter?.Invoke(_saGrid);
                selectionSetter?.Invoke(updateCounter);
            });
        }

        return Reactive(() =>
        {
            var currentGrid = Grid; // Access reactive grid signal
            // Also depend on the selection/update signal so any grid state
            // change (filters, pagination, etc.) re-renders the body/footer
            var renderCounter = _selectionSignal?.Item1();

            // Root container (stable layout) with header/body/footer
            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto")
            };

            // Recreate header reactively to avoid reparenting exceptions
            var header = _headerRenderer.CreateHeader(currentGrid);
            if (header is Control hdrCtrl)
            {
                hdrCtrl.SetValue(Panel.ZIndexProperty, 1);
                if (hdrCtrl is Panel hdrPanel)
                {
                    hdrPanel.Background = Brushes.White;
                }
            }
            Avalonia.Controls.Grid.SetRow(header, 0);
            var body = _bodyRenderer.CreateBody(currentGrid, () => Grid, _selectionSignal?.Item1);
            Avalonia.Controls.Grid.SetRow(body, 1);
            var footer = _footerRenderer.CreateFooter(currentGrid);
            Avalonia.Controls.Grid.SetRow(footer, 2);

            grid.Children.Add(header);
            grid.Children.Add(body);
            grid.Children.Add(footer);

            var mainBorder = new Border()
                .BorderThickness(1)
                .BorderBrush(Brushes.Gray)
                .Child(grid);

            // Note: Keyboard navigation remains disabled here to prioritize typing in filter inputs

            // Do not force focus on pointer press; let child controls manage focus naturally

            return mainBorder;
        });
    }
}

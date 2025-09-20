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
    private int _updateCounter = 0;

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
        
        // Initialize signal once for potential external reactivity (not used to rebuild root)
        if (_gridSignal == null)
        {
            _gridSignal = CreateSignal(_saGrid);
        }

        // Create stable header once to preserve TextBox focus and content
        var stableHeader = _headerRenderer.CreateHeader(_saGrid);
        // Make header clearly on top and hit-testable
        if (stableHeader is Control hdrCtrl)
        {
            hdrCtrl.SetValue(Panel.ZIndexProperty, 1);
            if (hdrCtrl is Panel hdrPanel)
            {
                hdrPanel.Background = Brushes.White;
            }
        }

        // Hosts for reactive parts
        var bodyHost = new ContentControl();
        var footerHost = new ContentControl();

        // Initial content
        bodyHost.Content = _bodyRenderer.CreateBody(_saGrid, () => _saGrid, () => _updateCounter);
        footerHost.Content = _footerRenderer.CreateFooter(_saGrid);

        // Root container (stable) with proper layout to avoid overlaps
        var grid = new Grid();
        grid.RowDefinitions = new RowDefinitions("Auto,*,Auto");

        // Place controls in rows
        Avalonia.Controls.Grid.SetRow(stableHeader, 0);
        Avalonia.Controls.Grid.SetRow(bodyHost, 1);
        Avalonia.Controls.Grid.SetRow(footerHost, 2);
        grid.Children.Add(stableHeader);
        grid.Children.Add(bodyHost);
        grid.Children.Add(footerHost);

        var mainBorder = new Border()
            .BorderThickness(1)
            .BorderBrush(Brushes.Gray)
            .Child(grid);

        // Note: Keyboard navigation temporarily disabled to ensure TextBox input works reliably.

        // Update reactive parts without rebuilding root or header
        _saGrid.SetUIUpdateCallback(() =>
        {
            _updateCounter++;
            Dispatcher.UIThread.Post(() =>
            {
                bodyHost.Content = _bodyRenderer.CreateBody(_saGrid, () => _saGrid, () => _updateCounter);
                footerHost.Content = _footerRenderer.CreateFooter(_saGrid);
            });
        });

        // Do not force focus on pointer press; let child controls manage focus naturally

        return mainBorder;
    }
}

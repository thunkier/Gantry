using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Gantry.UI.Shell.ViewModels;
using System;
using System.Globalization;

namespace Gantry.UI.Shell.Docking;

public partial class DockPaneView : UserControl
{
    public static readonly FuncValueConverter<DockSplitOrientation, bool> IsHorizontal =
        new FuncValueConverter<DockSplitOrientation, bool>(orientation => orientation == DockSplitOrientation.Horizontal);

    public static readonly FuncValueConverter<DockSplitOrientation, bool> IsVertical =
        new FuncValueConverter<DockSplitOrientation, bool>(orientation => orientation == DockSplitOrientation.Vertical);

    private TabDragHandler? _dragHandler;
    private ListBox? _tabListBox;

    public DockPaneView()
    {
        InitializeComponent();

        // Initialize drag handler when loaded
        this.Loaded += OnLoaded;
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Find the ListBox
        _tabListBox = this.FindControl<ListBox>("TabListBox");
        UpdateDragHandler();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UpdateDragHandler();
    }

    private void UpdateDragHandler()
    {
        if (DataContext is DockPaneViewModel pane && _tabListBox != null)
        {
            _dragHandler = new TabDragHandler(pane.Tabs, _tabListBox);
            _dragHandler.TabDetachRequested += OnTabDetachRequested;
        }
    }

    private void OnTabDetachRequested(object? sender, TabViewModel tab)
    {
        // Bubble up the event to MainWindowViewModel
        TabDetachRequested?.Invoke(this, tab);
    }

    private void OnChildTabDetachRequested(object? sender, TabViewModel tab)
    {
        // Bubble up event from child pane
        TabDetachRequested?.Invoke(this, tab);
    }

    /// <summary>
    /// Event raised when a tab should be detached to a new window.
    /// </summary>
    public event EventHandler<TabViewModel>? TabDetachRequested;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Event handlers for drag-and-drop
    public void TabItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragHandler?.OnPointerPressed(sender, e);
    }

    public void TabItem_PointerMoved(object? sender, PointerEventArgs e)
    {
        _dragHandler?.OnPointerMoved(sender, e);
    }

    public void TabItem_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragHandler?.OnPointerReleased(sender, e);
    }
}

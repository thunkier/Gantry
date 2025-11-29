using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Gantry.UI.Shell.ViewModels;
using Gantry.UI.Features.Collections.ViewModels;
using Gantry.UI.Features.Requests.ViewModels;
using Gantry.UI.Features.NodeEditor.ViewModels;
using Gantry.UI.Interfaces;

namespace Gantry.UI.Shell.Views;

public partial class Sidebar : UserControl
{
    private const double DragThreshold = 5.0; // Minimum pixels to move before starting drag
    private Point? _dragStartPoint;
    private Control? _draggedControl;
    private bool _isDragging;

    public Sidebar()
    {
        InitializeComponent();

        // Subscribe to drag-drop events using AddHandler
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);

        // Subscribe to KeyDown for canceling drag with Escape
        KeyDown += OnKeyDown;
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && DataContext is SidebarViewModel vm)
        {
            var editedItem = textBox.DataContext;
            if (editedItem != null)
            {
                vm.CompleteEditCommand.Execute(editedItem);
            }
        }
    }

    private void OnTextBoxAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // Focus and select all text when attached
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                textBox.Focus();
                textBox.SelectAll();
            }, Avalonia.Threading.DispatcherPriority.Loaded);

            // Handle keyboard events
            textBox.KeyDown += OnTextBoxKeyDown;
        }
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox && DataContext is SidebarViewModel vm)
        {
            var editedItem = textBox.DataContext;
            if (editedItem == null) return;

            if (e.Key == Key.Enter)
            {
                vm.CompleteEditCommand.Execute(editedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                vm.CancelEditCommand.Execute(editedItem);
                e.Handled = true;
            }
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Cancel drag with Escape
        if (e.Key == Key.Escape && _isDragging)
        {
            ResetDragState();
            e.Handled = true;
        }
    }

    // Drag and Drop Implementation with Threshold
    private void OnPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: ITreeItemViewModel item } control &&
            DataContext is SidebarViewModel vm &&
            e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            // Store the start point and control - don't start drag yet
            _dragStartPoint = e.GetPosition(this);
            _draggedControl = control;

            // Subscribe to pointer moved to detect drag threshold
            control.PointerMoved += OnPointerMovedDuringPress;
            control.PointerReleased += OnPointerReleasedBeforeDrag;
        }
    }

    private void OnPointerMovedDuringPress(object? sender, PointerEventArgs e)
    {
        if (_dragStartPoint == null || _draggedControl == null || _isDragging)
            return;

        var currentPoint = e.GetPosition(this);
        var distance = Point.Distance(_dragStartPoint.Value, currentPoint);

        // Only start drag if moved beyond threshold
        if (distance > DragThreshold &&
            _draggedControl.DataContext is ITreeItemViewModel item &&
            DataContext is SidebarViewModel vm)
        {
            // Unsubscribe from these events
            if (sender is Control control)
            {
                control.PointerMoved -= OnPointerMovedDuringPress;
                control.PointerReleased -= OnPointerReleasedBeforeDrag;
            }

            _isDragging = true;

            // Add visual feedback
            if (_draggedControl is StackPanel panel)
            {
                panel.Opacity = 0.6;
            }

            vm.StartDrag(item);

            // Create drag data using the new DataTransfer API
            // We use a simple text marker since the actual item is tracked in the ViewModel
            var dragData = new DataTransfer();
            var dataItem = new DataTransferItem();
            dataItem.SetText("tree-item");
            dragData.Add(dataItem);

            // Start async drag operation
            _ = Task.Run(async () =>
            {
                try
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await DragDrop.DoDragDropAsync(e, dragData, DragDropEffects.Move);
                        ResetDragState();
                    });
                }
                catch
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(ResetDragState);
                }
            });
        }
    }

    private void OnPointerReleasedBeforeDrag(object? sender, PointerReleasedEventArgs e)
    {
        // User released before threshold - not a drag
        if (sender is Control control)
        {
            control.PointerMoved -= OnPointerMovedDuringPress;
            control.PointerReleased -= OnPointerReleasedBeforeDrag;
        }

        ResetDragState();
    }

    private void ResetDragState()
    {
        if (_draggedControl is StackPanel panel)
        {
            panel.Opacity = 1.0;
        }

        _dragStartPoint = null;
        _draggedControl = null;
        _isDragging = false;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        HandleDragOver(sender, e, isEnter: true);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        HandleDragOver(sender, e, isEnter: false);
    }

    private void HandleDragOver(object? sender, DragEventArgs e, bool isEnter)
    {
        // Find the target item
        if (e.Source is Control control)
        {
            var target = FindTreeItemViewModel(control);

            if (target != null && target is CollectionViewModel && DataContext is SidebarViewModel vm)
            {
                if (vm.CanDrop(target))
                {
                    e.DragEffects = DragDropEffects.Move;

                    // Add visual feedback on enter with smoother color
                    if (isEnter)
                    {
                        var panel = FindStackPanel(control);
                        if (panel != null)
                        {
                            // Use a border instead of just background for better visibility
                            panel.Background = new Avalonia.Media.SolidColorBrush(
                                Avalonia.Media.Color.FromArgb(40, 100, 149, 237));
                        }
                    }
                    e.Handled = true;
                    return;
                }
            }
        }

        e.DragEffects = DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (e.Source is Control control)
        {
            var panel = FindStackPanel(control);
            if (panel != null)
            {
                panel.Background = Avalonia.Media.Brushes.Transparent;
            }
        }
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        // Clear visual feedback
        if (e.Source is Control control)
        {
            var panel = FindStackPanel(control);
            if (panel != null)
            {
                panel.Background = Avalonia.Media.Brushes.Transparent;
            }

            var target = FindTreeItemViewModel(control);

            if (target != null && e.DataTransfer.Contains(DataFormat.Text) && DataContext is SidebarViewModel vm)
            {
                // Verify it's our drag operation by checking the text content
                var text = e.DataTransfer.TryGetText();
                if (text == "tree-item")
                    ResetDragState();
            }
        }
    }

    private ITreeItemViewModel? FindTreeItemViewModel(Control control)
    {
        // Walk up the logical tree to find a control with ITreeItemViewModel DataContext
        var current = control as ILogical;
        while (current != null)
        {
            if (current is Control { DataContext: ITreeItemViewModel item })
            {
                return item;
            }
            current = current.LogicalParent;
        }
        return null;
    }

    private StackPanel? FindStackPanel(Control control)
    {
        // Walk up the logical tree to find the StackPanel
        var current = control as ILogical;
        while (current != null)
        {
            if (current is StackPanel panel)
            {
                return panel;
            }
            current = current.LogicalParent;
        }
        return null;
    }

    // Custom Resizing Logic
    private bool _isResizing;
    private Point _resizeStartPoint;
    private double _startSidebarWidth;

    private void OnResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && DataContext is SidebarViewModel vm)
        {
            _isResizing = true;
            _resizeStartPoint = e.GetPosition(this);
            _startSidebarWidth = vm.SidebarWidth.Value;
            e.Pointer.Capture(sender as Control);
            e.Handled = true;
        }
    }

    private void OnResizeHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isResizing || DataContext is not SidebarViewModel vm) return;

        var currentPoint = e.GetPosition(this);
        var delta = currentPoint.X - _resizeStartPoint.X;

        var newWidth = _startSidebarWidth + delta;

        // Enforce limits
        if (newWidth < 170) newWidth = 170;
        if (newWidth > 600) newWidth = 600;

        vm.SidebarWidth = new Avalonia.Controls.GridLength(newWidth);
        e.Handled = true;
    }

    private void OnResizeHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }
}

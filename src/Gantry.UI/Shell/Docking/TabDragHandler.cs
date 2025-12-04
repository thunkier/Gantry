using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading; // Required for Dispatcher
using Avalonia.VisualTree;
using System;
using System.Collections.ObjectModel;
using Gantry.UI.Shell.ViewModels;

namespace Gantry.UI.Shell.Docking;

public class TabDragHandler
{
    private const double DragThreshold = 5;
    
    private readonly ObservableCollection<TabViewModel> _tabs;
    private readonly ListBox _listBox;
    
    private TabViewModel? _draggedTab;
    private Control? _draggedContainer; // The visual tab item
    private DragGhostWindow? _ghostWindow;
    private Visual? _rootVisual; 
    
    private Point _dragStartPosition;
    private Point _ghostOffset;
    private bool _isDragging;
    
    // Throttle moves to prevent layout thrashing (Lag Fix)
    private DateTime _lastMoveTime = DateTime.MinValue;
    private const int MoveCooldownMs = 75; 

    public event EventHandler<TabViewModel>? TabDetachRequested;

    public TabDragHandler(ObservableCollection<TabViewModel> tabs, ListBox listBox)
    {
        _tabs = tabs;
        _listBox = listBox;
    }

    public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control sourceControl || sourceControl.DataContext is not TabViewModel tab)
            return;

        var point = e.GetCurrentPoint(_listBox); // Get point relative to ListBox
        
        if (point.Properties.IsLeftButtonPressed)
        {
            // 1. Identify what we are dragging
            _draggedTab = tab;
            _draggedContainer = _listBox.ContainerFromIndex(_tabs.IndexOf(tab)) as Control ?? sourceControl;
            
            _dragStartPosition = point.Position;
            
            // Calculate offset relative to the ItemContainer, not the clicked element
            // This ensures the ghost snaps to the top-left of the tab properly
            var itemPos = e.GetPosition(_draggedContainer);
            _ghostOffset = itemPos;
            
            _rootVisual = _listBox.GetVisualRoot() as Visual;
            _isDragging = false;
            
            // 2. CRITICAL FIX: Capture the LISTBOX, not the Tab Item.
            // The Tab Item might be destroyed/recreated during reordering.
            // The ListBox stays alive.
            e.Pointer.Capture(_listBox);
        }
    }

    public void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedTab == null) return;

        var currentPos = e.GetPosition(_listBox);

        // 1. Detect Drag Start
        if (!_isDragging)
        {
            var delta = currentPos - _dragStartPosition;
            if (Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) > DragThreshold)
            {
                StartDrag();
            }
        }

        // 2. Perform Drag
        if (_isDragging)
        {
            UpdateGhostPosition(e);
            HandleReordering(currentPos);
        }
    }

    public void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // 1. Release capture from the ListBox
        e.Pointer.Capture(null);

        // 2. Handle Drop Logic
        if (_isDragging && _draggedTab != null)
        {
            if (IsPointerOutsideListBox(e.GetPosition(_listBox)))
            {
                // Detach
                TabDetachRequested?.Invoke(this, _draggedTab);
                _tabs.Remove(_draggedTab);
            }
        }

        StopDrag();
    }

    private void StartDrag()
    {
        if (_draggedContainer == null) return;

        _isDragging = true;

        // Snapshot
        try 
        {
            var pixelSize = new PixelSize((int)_draggedContainer.Bounds.Width, (int)_draggedContainer.Bounds.Height);
            if (pixelSize.Width > 0 && pixelSize.Height > 0)
            {
                var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
                bitmap.Render(_draggedContainer);
                _ghostWindow = new DragGhostWindow(bitmap, _draggedContainer.Bounds.Width, _draggedContainer.Bounds.Height);
                _ghostWindow.Show();
            }
        }
        catch { /* Handle edge case where size is 0 or bitmap fails */ }

        // Hide real tab
        _draggedContainer.Opacity = 0.0;
    }

    private void StopDrag()
    {
        _ghostWindow?.Close();
        _ghostWindow = null;

        // Restore opacity if the container still exists
        if (_draggedContainer != null)
        {
            _draggedContainer.Opacity = 1.0;
        }
        // Also restore opacity of the tab at the current index (in case container ref changed)
        if (_draggedTab != null)
        {
             var index = _tabs.IndexOf(_draggedTab);
             if (index >= 0)
             {
                 var container = _listBox.ContainerFromIndex(index) as Control;
                 if (container != null) container.Opacity = 1.0;
             }
        }

        _listBox.Cursor = Cursor.Default;
        _draggedTab = null;
        _draggedContainer = null;
        _rootVisual = null;
        _isDragging = false;
    }

    private void UpdateGhostPosition(PointerEventArgs e)
    {
        if (_ghostWindow == null || _rootVisual == null) return;

        var rootPoint = e.GetPosition(_rootVisual);
        var screenPoint = _rootVisual.PointToScreen(rootPoint);

        // Add 1px offset to ensure mouse isn't hovering the window pixel-perfectly
        // which helps preventing input blocking on some OS configurations
        var x = screenPoint.X - (int)_ghostOffset.X + 2;
        var y = screenPoint.Y - (int)_ghostOffset.Y + 2;

        _ghostWindow.Position = new PixelPoint(x, y);
    }

    private void HandleReordering(Point position)
    {
        if (IsPointerOutsideListBox(position)) return;

        // Throttling to prevent lag
        if ((DateTime.Now - _lastMoveTime).TotalMilliseconds < MoveCooldownMs) return;

        int fromIndex = _tabs.IndexOf(_draggedTab!);
        int toIndex = GetDropIndex(position);

        if (fromIndex >= 0 && toIndex >= 0 && fromIndex != toIndex)
        {
            // Restore opacity of the "old" slot immediately before moving
            if (_draggedContainer != null) _draggedContainer.Opacity = 1.0;

            _tabs.Move(fromIndex, toIndex);
            _lastMoveTime = DateTime.Now;

            // Wait for UI to update, then hide the "new" slot
            Dispatcher.UIThread.Post(() => 
            {
                var newContainer = _listBox.ContainerFromIndex(toIndex) as Control;
                if (newContainer != null)
                {
                    _draggedContainer = newContainer;
                    _draggedContainer.Opacity = 0.0;
                }
            }, DispatcherPriority.Input);
        }
    }

    private bool IsPointerOutsideListBox(Point p)
    {
        var bounds = _listBox.Bounds;
        // Check if we are physically outside the bounds of the listbox area
        // Allow a small vertical buffer (25px) for "sloppy" dragging
        bool insideX = p.X >= 0 && p.X <= bounds.Width;
        bool insideY = p.Y >= -10 && p.Y <= bounds.Height + 25; 
        return !(insideX && insideY);
    }

    private int GetDropIndex(Point position)
    {
        for (int i = 0; i < _listBox.ItemCount; i++)
        {
            if (_listBox.ContainerFromIndex(i) is Control container)
            {
                var bounds = container.Bounds;
                if (position.X < bounds.X + (bounds.Width / 2))
                {
                    return i;
                }
            }
        }
        return _tabs.Count - 1;
    }
}
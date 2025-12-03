using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using System;
using System.Collections.ObjectModel;
using Gantry.UI.Shell.ViewModels;

namespace Gantry.UI.Shell.Docking;

/// <summary>
/// Handles drag-and-drop operations for tab reordering and detachment.
/// </summary>
public class TabDragHandler
{
    private const double DragThreshold = 5;
    private const double DetachThresholdY = 50; // Vertical distance to trigger detachment

    private TabViewModel? _draggedTab;
    private int _draggedIndex = -1;
    private Avalonia.Point _dragStartPosition;
    private bool _isDragging;
    private readonly ObservableCollection<TabViewModel> _tabs;
    private readonly ListBox _listBox;
    private Control? _draggedElement;

    /// <summary>
    /// Event raised when a tab should be detached to a new window.
    /// </summary>
    public event EventHandler<TabViewModel>? TabDetachRequested;

    public TabDragHandler(ObservableCollection<TabViewModel> tabs, ListBox listBox)
    {
        _tabs = tabs;
        _listBox = listBox;
    }

    public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
            return;

        var point = e.GetCurrentPoint(control);
        if (point.Properties.IsLeftButtonPressed)
        {
            _draggedElement = control;
            _dragStartPosition = point.Position;
            _isDragging = false;

            // Capture pointer to ensure we get events even if mouse leaves the control
            e.Pointer.Capture(control);

            // Find the tab being dragged
            if (control.DataContext is TabViewModel tab)
            {
                _draggedTab = tab;
                _draggedIndex = _tabs.IndexOf(tab);
            }
        }
    }

    public void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedTab == null || _draggedElement == null)
            return;

        var point = e.GetCurrentPoint(_draggedElement);
        var delta = point.Position - _dragStartPosition;
        var distance = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);

        // Start dragging if moved more than threshold
        if (!_isDragging && distance > DragThreshold)
        {
            _isDragging = true;
            // Reduce opacity to indicate dragging
            _draggedElement.Opacity = 0.6;
        }

        if (_isDragging)
        {
            // Check if dragged vertically beyond detachment threshold
            if (Math.Abs(delta.Y) > DetachThresholdY)
            {
                // Request tab detachment
                TabDetachRequested?.Invoke(this, _draggedTab);

                // Remove from current pane
                if (_draggedIndex >= 0 && _draggedIndex < _tabs.Count)
                {
                    _tabs.RemoveAt(_draggedIndex);
                }

                ResetDrag();
                return;
            }

            // Update cursor
            _listBox.Cursor = new Cursor(StandardCursorType.Hand);

            // Check for drag to reorder
            var position = e.GetPosition(_listBox);
            UpdateDropTarget(position);
        }
    }

    public void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggedElement != null)
        {
            e.Pointer.Capture(null);
        }

        if (!_isDragging || _draggedTab == null)
        {
            ResetDrag();
            return;
        }

        // Find drop index based on pointer position
        var position = e.GetPosition(_listBox);
        int dropIndex = GetDropIndex(position);

        if (_draggedIndex >= 0 && dropIndex >= 0 && _draggedIndex != dropIndex)
        {
            // Adjust drop index if we're moving the tab forward
            // (removing it first shifts all subsequent indices down by 1)
            int adjustedDropIndex = dropIndex;
            if (dropIndex > _draggedIndex)
            {
                adjustedDropIndex--;
            }

            // Reorder tabs
            _tabs.Move(_draggedIndex, adjustedDropIndex);
        }

        ResetDrag();
    }

    private void UpdateDropTarget(Avalonia.Point position)
    {
        // Visual feedback for drop position could be added here
    }

    private int GetDropIndex(Avalonia.Point position)
    {
        // Find which tab the pointer is over
        for (int i = 0; i < _listBox.ItemCount; i++)
        {
            var container = _listBox.ContainerFromIndex(i);
            if (container is Control itemControl)
            {
                var bounds = itemControl.Bounds;
                
                // Check if pointer is within the horizontal bounds of the item
                // We use a slightly wider hit test to make it easier to drop between items
                if (position.X >= bounds.X && position.X <= bounds.X + bounds.Width)
                {
                    var midPoint = bounds.X + bounds.Width / 2;
                    // If on the left half, insert at current index (before item)
                    // If on the right half, insert at next index (after item)
                    return position.X < midPoint ? i : i + 1;
                }
            }
        }

        // Handle edge cases
        if (_listBox.ItemCount > 0)
        {
            var first = _listBox.ContainerFromIndex(0) as Control;
            var last = _listBox.ContainerFromIndex(_listBox.ItemCount - 1) as Control;

            if (first != null && position.X < first.Bounds.X) return 0;
            if (last != null && position.X > last.Bounds.X + last.Bounds.Width) return _listBox.ItemCount;
        }

        return _draggedIndex; // No change if not found
    }

    private void ResetDrag()
    {
        if (_draggedElement != null)
        {
            _draggedElement.Opacity = 1.0;
        }
        _listBox.Cursor = Cursor.Default;
        _draggedTab = null;
        _draggedIndex = -1;
        _draggedElement = null;
        _isDragging = false;
    }
}

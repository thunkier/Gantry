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
            _tabs.RemoveAt(_draggedIndex);

            // Clamp to valid range
            adjustedDropIndex = Math.Max(0, Math.Min(adjustedDropIndex, _tabs.Count));

            _tabs.Insert(adjustedDropIndex, _draggedTab);
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
                var relativePoint = position;

                // Check if pointer is over this item
                if (relativePoint.X >= bounds.X && relativePoint.X <= bounds.X + bounds.Width)
                {
                    // Determine if we should insert before or after
                    var midPoint = bounds.X + bounds.Width / 2;
                    return relativePoint.X < midPoint ? i : i + 1;
                }
            }
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

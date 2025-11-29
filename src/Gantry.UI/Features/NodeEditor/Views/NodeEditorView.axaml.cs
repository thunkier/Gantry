using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Gantry.UI.Features.NodeEditor.ViewModels;
using Gantry.UI.Features.Collections.ViewModels;
using Gantry.UI.Features.Requests.ViewModels;
using Gantry.UI.Interfaces;
using System;
using System.Linq;

namespace Gantry.UI.Features.NodeEditor.Views;

public partial class NodeEditorView : UserControl
{
    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartX;
    private double _panStartY;

    public NodeEditorView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);

        this.PointerMoved += OnPointerMoved;
        this.PointerWheelChanged += OnPointerWheelChanged;
        this.PointerPressed += OnPointerPressed;
        this.PointerReleased += OnPointerReleased;
        this.KeyDown += OnKeyDown;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not NodeEditorViewModel vm) return;

        // Zoom with mouse wheel
        var delta = e.Delta.Y > 0 ? 1.1 : 0.9;
        var mousePos = e.GetPosition(this);

        var oldZoom = vm.ZoomScale;
        var newZoom = Math.Clamp(oldZoom * delta, 0.1, 5.0);

        // Zoom towards mouse position
        var zoomFactor = newZoom / oldZoom;
        vm.PanX = mousePos.X - (mousePos.X - vm.PanX) * zoomFactor;
        vm.PanY = mousePos.Y - (mousePos.Y - vm.PanY) * zoomFactor;
        vm.ZoomScale = newZoom;

        e.Handled = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not NodeEditorViewModel vm) return;

        var point = e.GetCurrentPoint(this);

        // Check if we clicked on the background (not on a node)
        var hitElement = this.InputHitTest(e.GetPosition(this));
        var clickedOnBackground = hitElement is Panel or GridBackgroundControl;

        // Middle mouse button or Shift+Left for panning, OR Left-click on background
        if (point.Properties.IsMiddleButtonPressed ||
            (point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) ||
            (point.Properties.IsLeftButtonPressed && clickedOnBackground))
        {
            _isPanning = true;
            _panStartPoint = e.GetPosition(this);
            _panStartX = vm.PanX;
            _panStartY = vm.PanY;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not NodeEditorViewModel vm) return;

        // Handle Delete key
        if (e.Key == Key.Delete)
        {
            // Delete selected nodes
            var selectedNodes = vm.Nodes.Where(n => n.IsSelected).ToList();
            foreach (var node in selectedNodes)
            {
                vm.RemoveNodeCommand.Execute(node);
            }

            // Delete selected connections (if we add selection to connections later)
            var selectedConnections = vm.Connections.Where(c => c.IsSelected).ToList();
            foreach (var conn in selectedConnections)
            {
                vm.Connections.Remove(conn);
            }

            e.Handled = true;
        }
        // Handle Ctrl+A (Select All)
        else if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            foreach (var node in vm.Nodes)
            {
                node.IsSelected = true;
            }
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is NodeEditorViewModel vm)
        {
            // Handle panning
            if (_isPanning)
            {
                var currentPoint = e.GetPosition(this);
                var delta = currentPoint - _panStartPoint;
                vm.PanX = _panStartX + delta.X;
                vm.PanY = _panStartY + delta.Y;
            }

            // Update mouse position - transform to account for zoom/pan
            var transformPanel = this.FindControl<Panel>("TransformPanel");
            if (transformPanel != null)
            {
                // Get position relative to the transformed panel
                var posInTransformedSpace = e.GetPosition(transformPanel);
                vm.MousePosition = posInTransformedSpace;
            }
            else
            {
                // Fallback to untransformed position
                vm.MousePosition = e.GetPosition(this);
            }
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        // Note: Using Data (IDataObject) as DataTransfer might not have Contains directly or might behave differently.
        // But assuming standard replacement:
        if (e.Data.Contains("Context"))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is NodeEditorViewModel vm && e.Data.Get("Context") is RequestItemViewModel requestVm)
        {
            var position = e.GetPosition(this);
            vm.DropCommand.Execute(new NodeDropArgs(requestVm, position.X, position.Y));
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

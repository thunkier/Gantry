using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Gantry.UI.Features.NodeEditor.ViewModels;
using System;
using System.Linq;

namespace Gantry.UI.Features.NodeEditor.Views;

public partial class NodeView : UserControl
{
    private bool _isDraggingNode;
    private Point _dragStartPoint;
    private Point _nodeStartPosition;

    public NodeView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (s, e) => Dispatcher.UIThread.Post(UpdatePinOffsets, DispatcherPriority.Loaded);
        this.LayoutUpdated += (s, e) => UpdatePinOffsets(); // Ensure offsets stay correct if node resizes
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // 1. Unified Pointer Pressed Handler
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (DataContext is not NodeViewModel nodeVm) return;

        var editorView = this.FindAncestorOfType<NodeEditorView>();
        var editorVm = editorView?.DataContext as NodeEditorViewModel;
        if (editorView == null || editorVm == null) return;

        // CHECK: Did we click a Pin?
        var source = e.Source as Visual;
        var pinConnector = source?.FindAncestorOfType<Border>(true);

        if (pinConnector?.Name == "PinConnector" && pinConnector.Tag is PinViewModel pin)
        {
            // --- PIN LOGIC ---
            if (pin.Type == PinType.Output)
            {
                UpdatePinOffsets(); // Ensure math is fresh
                editorVm.StartConnectionDrag(pin);
                e.Handled = true;
            }
            return; // Stop here, don't drag the node
        }

        // --- NODE SELECTION LOGIC ---
        var isCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (!isCtrl)
        {
            foreach (var n in editorVm.Nodes) if (n != nodeVm) n.IsSelected = false;
        }
        nodeVm.IsSelected = !nodeVm.IsSelected || !isCtrl;

        // --- NODE DRAG LOGIC ---
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDraggingNode = true;
            _dragStartPoint = e.GetPosition(null); // Screen Coords
            _nodeStartPosition = new Point(nodeVm.X, nodeVm.Y);
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    // 2. Unified Pointer Moved Handler
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isDraggingNode && DataContext is NodeViewModel nodeVm)
        {
            var currentPoint = e.GetPosition(null);
            var delta = currentPoint - _dragStartPoint;

            nodeVm.X = _nodeStartPosition.X + delta.X;
            nodeVm.Y = _nodeStartPosition.Y + delta.Y;

            e.Handled = true;
        }
    }

    // 3. Unified Pointer Released Handler
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // Handle Node Drop
        if (_isDraggingNode)
        {
            _isDraggingNode = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        // Handle Pin Drop (Connecting)
        var editorView = this.FindAncestorOfType<NodeEditorView>();
        var editorVm = editorView?.DataContext as NodeEditorViewModel;

        if (editorVm?.PendingConnectionSource != null)
        {
            // Did we drop onto a pin?
            var source = e.Source as Visual;
            var pinConnector = source?.FindAncestorOfType<Border>(true);

            if (pinConnector?.Name == "PinConnector" && pinConnector.Tag is PinViewModel targetPin)
            {
                if (targetPin.Type == PinType.Input)
                {
                    // Update offset of target to ensure line ends at correct spot
                    var pinCenter = pinConnector.TranslatePoint(new Point(6, 6), this) ?? new Point(0, 0);
                    targetPin.Offset = pinCenter;

                    editorVm.CompleteConnectionDrag(targetPin);
                    e.Handled = true;
                }
            }
            else
            {
                // Dropped into empty space -> Cancel
                editorVm.PendingConnectionSource = null;
            }
        }
    }

    // 4. Helper to Calculate Pin Positions
    private void UpdatePinOffsets()
    {
        // Find all pin borders named "PinConnector"
        var pins = this.GetVisualDescendants()
                       .OfType<Border>()
                       .Where(b => b.Name == "PinConnector");

        foreach (var border in pins)
        {
            if (border.Tag is PinViewModel pinVm)
            {
                // Calculate center (6,6 assumes 12x12 pin size)
                // TranslatePoint gives position relative to This NodeView (0,0)
                var center = border.TranslatePoint(new Point(6, 6), this);
                if (center.HasValue)
                {
                    // Only update if changed to avoid loop
                    if (pinVm.Offset != center.Value)
                    {
                        pinVm.Offset = center.Value;
                    }
                }
            }
        }
    }
}

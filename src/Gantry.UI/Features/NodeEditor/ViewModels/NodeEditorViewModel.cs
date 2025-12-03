using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Gantry.UI.Features.NodeEditor.Services;
using Avalonia;

namespace Gantry.UI.Features.NodeEditor.ViewModels;

public partial class NodeEditorViewModel : ObservableObject
{
    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();

    [ObservableProperty]
    private Avalonia.Point _mousePosition;

    public System.Func<Task<Gantry.Core.Domain.Collections.RequestItem?>>? RequestSelector { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PendingStartPoint))] // Update StartPoint when Source changes
    private PinViewModel? _pendingConnectionSource;

    public Point PendingStartPoint => PendingConnectionSource?.Anchor ?? new Point(0, 0);

    [ObservableProperty]
    private double _zoomScale = 1.0;

    [ObservableProperty]
    private double _panX = 0;

    [ObservableProperty]
    private double _panY = 0;

    public void Pan(double dx, double dy)
    {
        PanX += dx;
        PanY += dy;
    }

    public void Zoom(double delta, double centerX, double centerY)
    {
        var oldZoom = ZoomScale;
        var newZoom = System.Math.Clamp(oldZoom * delta, 0.1, 5.0); // Limits 0.1x to 5x

        // Calculate offset to keep mouse pointed at same location during zoom
        // (Math omitted for brevity, handled in View for smoothness, or here if strict MVVM)
        ZoomScale = newZoom;
    }

    [RelayCommand]
    public void AddNode(NodeViewModel node)
    {
        Nodes.Add(node);
    }

    [RelayCommand]
    public void RemoveNode(NodeViewModel node)
    {
        // Remove associated connections
        var toRemove = Connections.Where(c => c.Source.Parent == node || c.Target.Parent == node).ToList();
        foreach (var conn in toRemove)
        {
            Connections.Remove(conn);
        }
        Nodes.Remove(node);
    }

    public void Connect(PinViewModel source, PinViewModel target)
    {
        if (source.Type == PinType.Output && target.Type == PinType.Input && source.Parent != target.Parent)
        {
            // Check if connection already exists
            if (!Connections.Any(c => c.Source == source && c.Target == target))
            {
                Connections.Add(new ConnectionViewModel(source, target));
            }
        }
    }

    public void StartConnectionDrag(PinViewModel source)
    {
        if (source.Type == PinType.Output)
        {
            PendingConnectionSource = source;
        }
    }

    public void CompleteConnectionDrag(PinViewModel target)
    {
        if (PendingConnectionSource != null && target.Type == PinType.Input)
        {
            Connect(PendingConnectionSource, target);
        }
        PendingConnectionSource = null;
    }

    private readonly Gantry.Core.Domain.NodeEditor.NodeGraph _graph;
    private readonly Gantry.Infrastructure.Services.WorkspaceService _workspaceService;

    public NodeEditorViewModel() : this(new Gantry.Core.Domain.NodeEditor.NodeGraph(), new Gantry.Infrastructure.Services.WorkspaceService())
    {
    }

    public NodeEditorViewModel(Gantry.Core.Domain.NodeEditor.NodeGraph graph, Gantry.Infrastructure.Services.WorkspaceService workspaceService)
    {
        _graph = graph;
        _workspaceService = workspaceService;

        // Load nodes
        foreach (var nodeModel in _graph.Nodes)
        {
            if (nodeModel.Type == "RequestNode" && nodeModel.RequestItem != null)
            {
                var vm = new RequestNodeViewModel(nodeModel.RequestItem, nodeModel.X, nodeModel.Y) { Id = nodeModel.Id };
                Nodes.Add(vm);
            }
            // Add other node types here
        }

        // Load connections
        foreach (var connModel in _graph.Connections)
        {
            var sourceNode = Nodes.FirstOrDefault(n => n.Id == connModel.SourceNodeId);
            var targetNode = Nodes.FirstOrDefault(n => n.Id == connModel.TargetNodeId);

            if (sourceNode != null && targetNode != null)
            {
                var sourcePin = sourceNode.Outputs.FirstOrDefault(p => p.Name == connModel.SourcePinId); // Assuming PinId is Name for now
                var targetPin = targetNode.Inputs.FirstOrDefault(p => p.Name == connModel.TargetPinId);

                if (sourcePin != null && targetPin != null)
                {
                    Connections.Add(new ConnectionViewModel(sourcePin, targetPin));
                }
            }
        }
    }

    [RelayCommand]
    private void Save()
    {
        _graph.Nodes.Clear();
        foreach (var node in Nodes)
        {
            var nodeModel = new Gantry.Core.Domain.NodeEditor.NodeModel
            {
                Id = node.Id,
                X = node.X,
                Y = node.Y
            };

            if (node is RequestNodeViewModel reqNode)
            {
                nodeModel.Type = "RequestNode";
                nodeModel.RequestItem = reqNode.RequestItem;
            }

            _graph.Nodes.Add(nodeModel);
        }

        _graph.Connections.Clear();
        foreach (var conn in Connections)
        {
            _graph.Connections.Add(new Gantry.Core.Domain.NodeEditor.ConnectionModel
            {
                SourceNodeId = conn.Source.Parent.Id,
                SourcePinId = conn.Source.Name,
                TargetNodeId = conn.Target.Parent.Id,
                TargetPinId = conn.Target.Name
            });
        }

        _workspaceService.Repository.SaveNodeGraph(_graph);
    }

    [RelayCommand]
    private async Task RunGraph()
    {
        var runner = new GraphRunner();
        await runner.RunGraphAsync(Nodes, Connections);
    }

    [RelayCommand]
    private void Drop(NodeDropArgs args)
    {
        var node = new RequestNodeViewModel(args.Request.Model, args.X, args.Y);
        Nodes.Add(node);
    }

    [RelayCommand]
    private async Task AddRequest()
    {
        if (RequestSelector != null)
        {
            var selectedRequest = await RequestSelector();
            if (selectedRequest != null)
            {
                var node = new RequestNodeViewModel(selectedRequest, 100, 100);
                Nodes.Add(node);
            }
        }
    }
    public void AddRequestFromPath(string path, double x, double y)
    {
        var request = _workspaceService.Repository.LoadRequest(path);
        if (request != null)
        {
            var node = new RequestNodeViewModel(request, x, y);
            Nodes.Add(node);
        }
    }
}

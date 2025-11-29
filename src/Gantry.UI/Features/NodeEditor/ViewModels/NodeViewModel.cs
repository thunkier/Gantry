using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.UI.Features.NodeEditor.Models;
using System.Collections.ObjectModel;

namespace Gantry.UI.Features.NodeEditor.ViewModels;

/// <summary>
/// Represents a node in the node editor graph.
/// </summary>
public partial class NodeViewModel : ObservableObject
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private double _width = 200;

    [ObservableProperty]
    private double _height = 150;

    [ObservableProperty]
    private bool _isCollapsed;

    [ObservableProperty]
    private string _nodeColor = "#2D2D2D";

    [ObservableProperty]
    private NodeStatus _status = NodeStatus.Idle;

    public ObservableCollection<PinViewModel> Inputs { get; } = new();
    public ObservableCollection<PinViewModel> Outputs { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeViewModel"/> class.
    /// </summary>
    /// <param name="title">The title of the node.</param>
    /// <param name="x">The X position of the node.</param>
    /// <param name="y">The Y position of the node.</param>
    public NodeViewModel(string title, double x, double y)
    {
        Title = title;
        X = x;
        Y = y;
    }

    /// <summary>
    /// Adds an input pin to the node.
    /// </summary>
    /// <param name="name">The name of the input pin.</param>
    /// <param name="dataType">The data type of the pin.</param>
    public void AddInput(string name, DataType dataType = DataType.Any)
    {
        Inputs.Add(new PinViewModel(name, PinType.Input, this, dataType));
    }

    /// <summary>
    /// Adds an output pin to the node.
    /// </summary>
    /// <param name="name">The name of the output pin.</param>
    /// <param name="dataType">The data type of the pin.</param>
    public void AddOutput(string name, DataType dataType = DataType.Any)
    {
        Outputs.Add(new PinViewModel(name, PinType.Output, this, dataType));
    }
}

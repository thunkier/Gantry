using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.UI.Features.NodeEditor.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Gantry.UI.Features.NodeEditor.ViewModels;

public enum PinType
{
    Input,
    Output
}

/// <summary>
/// Represents a connection pin on a node.
/// </summary>
public partial class PinViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private PinType _type;

    [ObservableProperty]
    private NodeViewModel _parent;

    [ObservableProperty]
    private DataType _dataType;

    [ObservableProperty]
    private bool _allowMultipleConnections;

    // The position of the pin relative to the top-left of the Node
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Anchor))]
    private Point _offset;

    // The absolute position on the canvas (Node Position + Pin Offset)
    public Point Anchor => new(Parent.X + Offset.X, Parent.Y + Offset.Y);

    /// <summary>
    /// Gets the color for this pin based on its data type.
    /// </summary>
    public IBrush Color => DataType switch
    {
        DataType.Trigger => new SolidColorBrush(Colors.White),
        DataType.String => new SolidColorBrush(Avalonia.Media.Color.Parse("#E91E63")),    // Pink
        DataType.Number => new SolidColorBrush(Avalonia.Media.Color.Parse("#4CAF50")),    // Green
        DataType.Boolean => new SolidColorBrush(Avalonia.Media.Color.Parse("#FF9800")),   // Orange
        DataType.Object => new SolidColorBrush(Avalonia.Media.Color.Parse("#2196F3")),    // Blue
        DataType.Array => new SolidColorBrush(Avalonia.Media.Color.Parse("#9C27B0")),     // Purple
        DataType.Any => new SolidColorBrush(Avalonia.Media.Color.Parse("#9E9E9E")),       // Gray
        _ => new SolidColorBrush(Avalonia.Media.Color.Parse("#9E9E9E"))
    };

    // Track connections for validation
    private readonly List<PinViewModel> _connectedPins = new();
    public IReadOnlyList<PinViewModel> ConnectedPins => _connectedPins.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="PinViewModel"/> class.
    /// </summary>
    /// <param name="name">The name of the pin.</param>
    /// <param name="type">The type of the pin (Input or Output).</param>
    /// <param name="parent">The parent node.</param>
    /// <param name="dataType">The data type of the pin.</param>
    public PinViewModel(string name, PinType type, NodeViewModel parent, DataType dataType = DataType.Any)
    {
        Name = name;
        Type = type;
        Parent = parent;
        DataType = dataType;

        // Output pins can have multiple connections, input pins typically only one
        AllowMultipleConnections = type == PinType.Output;

        // Listen to the Parent's property changes.
        // If the Node moves (X or Y changes), we must notify the View 
        // that this Pin's absolute 'Anchor' has changed so the line redraws.
        Parent.PropertyChanged += OnParentPropertyChanged;
    }

    private void OnParentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NodeViewModel.X) ||
            e.PropertyName == nameof(NodeViewModel.Y))
        {
            OnPropertyChanged(nameof(Anchor));
        }
    }

    /// <summary>
    /// Checks if this pin can connect to another pin.
    /// </summary>
    /// <param name="other">The pin to check connection compatibility with.</param>
    /// <returns>True if connection is valid, false otherwise.</returns>
    public bool CanConnectTo(PinViewModel other)
    {
        // Can't connect to self
        if (other == this) return false;

        // Can't connect to same parent node
        if (other.Parent == Parent) return false;

        // Can't connect two pins of the same type
        if (other.Type == Type) return false;

        // Check if input already has a connection (unless multiple allowed)
        if (Type == PinType.Input && !AllowMultipleConnections && _connectedPins.Any())
            return false;

        if (other.Type == PinType.Input && !other.AllowMultipleConnections && other._connectedPins.Any())
            return false;

        // Check data type compatibility
        if (DataType != DataType.Any && other.DataType != DataType.Any && DataType != other.DataType)
            return false;

        return true;
    }

    /// <summary>
    /// Registers a connection with another pin.
    /// </summary>
    internal void AddConnection(PinViewModel other)
    {
        if (!_connectedPins.Contains(other))
        {
            _connectedPins.Add(other);
        }
    }

    /// <summary>
    /// Removes a connection with another pin.
    /// </summary>
    internal void RemoveConnection(PinViewModel other)
    {
        _connectedPins.Remove(other);
    }
}

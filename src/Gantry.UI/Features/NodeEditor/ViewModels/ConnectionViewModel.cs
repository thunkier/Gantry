using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Gantry.UI.Features.NodeEditor.ViewModels;

/// <summary>
/// Represents a connection between two pins in the node editor.
/// </summary>
public partial class ConnectionViewModel : ObservableObject
{
    [ObservableProperty]
    private PinViewModel _source;

    [ObservableProperty]
    private PinViewModel _target;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets whether this connection is valid based on pin types.
    /// </summary>
    public bool IsValid => Source.CanConnectTo(Target);

    /// <summary>
    /// Gets the color for this connection based on the source pin data type.
    /// </summary>
    public IBrush ConnectionColor => Source.Color;

    /// <summary>
    /// Calculates the Bezier curve path for the connection.
    /// </summary>
    public PathGeometry PathGeometry
    {
        get
        {
            var start = Source.Anchor;
            var end = Target.Anchor;

            // Calculate control points for a smooth horizontal Bezier curve
            var distance = end.X - start.X;
            var controlPointOffset = Math.Max(Math.Abs(distance) * 0.5, 50);

            var controlPoint1 = new Point(start.X + controlPointOffset, start.Y);
            var controlPoint2 = new Point(end.X - controlPointOffset, end.Y);

            var figure = new PathFigure
            {
                StartPoint = start,
                IsClosed = false
            };

            figure.Segments = new PathSegments
            {
                new BezierSegment
                {
                    Point1 = controlPoint1,
                    Point2 = controlPoint2,
                    Point3 = end
                }
            };

            var geometry = new PathGeometry();
            geometry.Figures = new PathFigures { figure };

            return geometry;
        }
    }

    public ConnectionViewModel(PinViewModel source, PinViewModel target)
    {
        Source = source;
        Target = target;

        // Track connections in pins
        Source.AddConnection(Target);
        Target.AddConnection(Source);

        // Listen to source and target anchor changes to update path
        Source.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PinViewModel.Anchor))
                OnPropertyChanged(nameof(PathGeometry));
        };

        Target.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PinViewModel.Anchor))
                OnPropertyChanged(nameof(PathGeometry));
        };
    }
}

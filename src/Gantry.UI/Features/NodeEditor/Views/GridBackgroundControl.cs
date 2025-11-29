using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gantry.UI.Features.NodeEditor.Views;

public class GridBackgroundControl : Control
{
    public static readonly StyledProperty<double> GridSizeProperty =
        AvaloniaProperty.Register<GridBackgroundControl, double>(nameof(GridSize), 20.0);

    public static readonly StyledProperty<double> ZoomScaleProperty =
        AvaloniaProperty.Register<GridBackgroundControl, double>(nameof(ZoomScale), 1.0);

    public static readonly StyledProperty<double> PanXProperty =
        AvaloniaProperty.Register<GridBackgroundControl, double>(nameof(PanX), 0.0);

    public static readonly StyledProperty<double> PanYProperty =
        AvaloniaProperty.Register<GridBackgroundControl, double>(nameof(PanY), 0.0);

    public double GridSize
    {
        get => GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    public double ZoomScale
    {
        get => GetValue(ZoomScaleProperty);
        set => SetValue(ZoomScaleProperty, value);
    }

    public double PanX
    {
        get => GetValue(PanXProperty);
        set => SetValue(PanXProperty, value);
    }

    public double PanY
    {
        get => GetValue(PanYProperty);
        set => SetValue(PanYProperty, value);
    }

    static GridBackgroundControl()
    {
        AffectsRender<GridBackgroundControl>(GridSizeProperty, ZoomScaleProperty, PanXProperty, PanYProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var gridSize = GridSize * ZoomScale;

        // Skip rendering if grid would be too dense
        if (gridSize < 5) return;

        // Calculate grid offset based on pan
        var offsetX = (PanX % gridSize + gridSize) % gridSize;
        var offsetY = (PanY % gridSize + gridSize) % gridSize;

        var dotBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)); // Semi-transparent white dots
        var dotRadius = 1.5;

        // Draw dots
        for (double x = offsetX; x < bounds.Width; x += gridSize)
        {
            for (double y = offsetY; y < bounds.Height; y += gridSize)
            {
                context.DrawEllipse(dotBrush, null, new Point(x, y), dotRadius, dotRadius);
            }
        }

        // Draw major grid lines every 5 dots (optional)
        if (gridSize >= 10)
        {
            var majorGridSize = gridSize * 5;
            var majorOffsetX = (PanX % majorGridSize + majorGridSize) % majorGridSize;
            var majorOffsetY = (PanY % majorGridSize + majorGridSize) % majorGridSize;

            var linePen = new Pen(new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), 1);

            // Vertical lines
            for (double x = majorOffsetX; x < bounds.Width; x += majorGridSize)
            {
                context.DrawLine(linePen, new Point(x, 0), new Point(x, bounds.Height));
            }

            // Horizontal lines
            for (double y = majorOffsetY; y < bounds.Height; y += majorGridSize)
            {
                context.DrawLine(linePen, new Point(0, y), new Point(bounds.Width, y));
            }
        }
    }
}

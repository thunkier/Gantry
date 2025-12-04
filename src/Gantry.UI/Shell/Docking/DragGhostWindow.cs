using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Gantry.UI.Shell.Docking;

public class DragGhostWindow : Window
{
    public DragGhostWindow(Bitmap tabImage, double width, double height)
    {
        SystemDecorations = SystemDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        
        // CRITICAL: These settings ensure the window is "invisible" to the mouse
        Focusable = false;
        ShowActivated = false;
        IsHitTestVisible = false; 
        Background = Brushes.Transparent;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        
        Width = width;
        Height = height;
        SizeToContent = SizeToContent.Manual;

        Content = new Image
        {
            Source = tabImage,
            Width = width,
            Height = height,
            Stretch = Stretch.Fill,
            Opacity = 0.85,
            IsHitTestVisible = false // Double check to ensure image doesn't catch clicks
        };
    }
}
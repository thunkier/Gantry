using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Gantry.UI.Shell.ViewModels;
using System;

namespace Gantry.UI.Shell.Docking;

public partial class DockableWorkspace : UserControl
{
    public event EventHandler<TabViewModel>? TabDetachRequested;

    public DockableWorkspace()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnTabDetachRequested(object? sender, TabViewModel tab)
    {
        TabDetachRequested?.Invoke(this, tab);
    }
}

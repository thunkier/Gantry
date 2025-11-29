using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Gantry.UI.Shell.ViewModels;
using System;

namespace Gantry.UI.Shell;

public partial class DetachedTabWindow : Window
{
    /// <summary>
    /// Event raised when the user clicks the re-dock button.
    /// </summary>
    public event EventHandler<TabViewModel>? RedockRequested;

    public DetachedTabWindow()
    {
        InitializeComponent();
    }

    public DetachedTabWindow(TabViewModel tab) : this()
    {
        DataContext = tab;
        Title = tab.Title;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void RedockButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TabViewModel tab)
        {
            RedockRequested?.Invoke(this, tab);
            Close();
        }
    }
}

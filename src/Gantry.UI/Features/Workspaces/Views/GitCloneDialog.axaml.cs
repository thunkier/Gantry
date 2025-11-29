using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Gantry.UI.Features.Workspaces.Views;

public partial class GitCloneDialog : Window
{
    public string GitUrl => this.FindControl<TextBox>("GitUrlTextBox")?.Text ?? string.Empty;

    public GitCloneDialog()
    {
        InitializeComponent();
    }

    private void OnCloneClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}

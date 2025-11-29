using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Gantry.UI.Features.Workspaces.Views;

public partial class CreateWorkspaceDialog : Window
{
    public string WorkspaceName { get; private set; } = string.Empty;

    public CreateWorkspaceDialog()
    {
        InitializeComponent();
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        var nameBox = this.FindControl<TextBox>("NameTextBox");
        if (!string.IsNullOrWhiteSpace(nameBox?.Text))
        {
            WorkspaceName = nameBox.Text;
            Close(true);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}

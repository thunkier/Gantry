using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Gantry.UI.Features.SourceControl.Views;

public partial class CreateRepositoryDialog : Window
{
    public bool CreateGitIgnore { get; set; } = true;
    public bool CreateReadme { get; set; } = true;

    public CreateRepositoryDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnInitializeClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}

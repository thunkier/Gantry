using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Gantry.UI.Shell.Views;

public partial class AppSettingsDialog : Window
{
    public AppSettingsDialog()
    {
        InitializeComponent();
        DataContext = new Gantry.UI.Shell.ViewModels.AppSettingsViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using Gantry.UI.Features.NodeEditor.ViewModels;

namespace Gantry.UI.Features.NodeEditor.Views;

public partial class AddRequestDialog : Window
{
    public AddRequestDialog()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddRequestDialogViewModel vm)
        {
            Close(vm.SelectedRequest);
        }
        else
        {
            Close(null);
        }
    }
}

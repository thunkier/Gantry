using Avalonia.Controls;
using Avalonia.Interactivity;
using Gantry.UI.Shell.ViewModels;
using Gantry.UI.Features.Collections.ViewModels;
using Gantry.UI.Features.Requests.ViewModels;
using Gantry.UI.Features.NodeEditor.ViewModels;
using Gantry.UI.Interfaces;

namespace Gantry.UI.Shell.Views;

public partial class MessageBox : Window
{
    public MessageBox()
    {
        InitializeComponent();
    }

    public MessageBox(string title, string message) : this()
    {
        DataContext = new MessageBoxViewModel(title, message);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

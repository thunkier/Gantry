using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gantry.UI.Features.Requests.Views;

public partial class RequestSettingsDialog : Window
{
    public RequestSettingsDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

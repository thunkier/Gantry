using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gantry.UI.Features.Requests.Views;

public partial class RequestSettingsView : UserControl
{
    public RequestSettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

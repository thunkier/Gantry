using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gantry.UI.Shell.ViewModels;

public partial class MessageBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Message";

    [ObservableProperty]
    private string _message = string.Empty;

    public MessageBoxViewModel(string title, string message)
    {
        Title = title;
        Message = message;
    }

    public MessageBoxViewModel() { }
}

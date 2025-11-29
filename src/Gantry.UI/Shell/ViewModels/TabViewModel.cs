using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gantry.UI.Shell.Docking;
using Gantry.UI.Features.Requests.ViewModels;
using Gantry.UI.Features.Collections.ViewModels;

namespace Gantry.UI.Shell.ViewModels;

public partial class TabViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "New Tab";

    [ObservableProperty]
    private bool _isDirty;

    [RelayCommand]
    public virtual void Close()
    {
        // Logic handled by parent or event
        CloseRequested?.Invoke(this, System.EventArgs.Empty);
    }

    public event System.EventHandler? CloseRequested;

    /// <summary>
    /// Converts a tab to a DTO for serialization.
    /// </summary>
    public static TabDto ToDto(TabViewModel tab)
    {
        var dto = new TabDto
        {
            Title = tab.Title,
            TabType = tab.GetType().Name
        };

        // Note: Currently only serializing basic tab info
        // Full tab restoration would require more context
        // For now, tabs will be recreated as empty on load

        return dto;
    }
}

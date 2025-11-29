using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.Core.Domain.Collections;
using Gantry.UI.Interfaces;
using System.Collections.ObjectModel;

namespace Gantry.UI.Features.Collections.ViewModels;

public partial class RequestItemViewModel : ObservableObject, ITreeItemViewModel
{
    public RequestItem Model { get; }

    public RequestItemViewModel(RequestItem model)
    {
        Model = model;
    }

    public string Name => Model.Name;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editableName = string.Empty;

    public string Method => Model.Request.Method;

    public string MethodColor => Model.Request.Method switch
    {
        "GET" => "Green",
        "POST" => "Blue",
        "PUT" => "Orange",
        "DELETE" => "Red",
        "PATCH" => "Purple",
        _ => "Gray"
    };

    public ObservableCollection<ITreeItemViewModel> Children { get; } = new();

    public void RaiseNameChanged()
    {
        OnPropertyChanged(nameof(Name));
    }
}

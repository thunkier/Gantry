using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.Core.Domain.Collections;

namespace Gantry.UI.Features.Requests.ViewModels;

public partial class HeaderViewModel : ObservableObject
{
    private readonly HeaderItem _model;

    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private bool _isAuto;

    [ObservableProperty]
    private bool _isReadOnly;

    public HeaderViewModel(HeaderItem model)
    {
        _model = model;
        Key = model.Key;
        Value = model.Value;
        Description = model.Description;
        IsActive = model.IsActive;
    }

    public HeaderItem ToModel()
    {
        _model.Key = Key;
        _model.Value = Value;
        _model.Description = Description;
        _model.IsActive = IsActive;
        return _model;
    }
}

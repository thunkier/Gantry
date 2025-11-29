using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.Core.Domain.Collections;

namespace Gantry.UI.Features.Requests.ViewModels;

public partial class ParamViewModel : ObservableObject
{
    private readonly ParamItem _model;

    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isActive = true;

    public ParamViewModel(ParamItem model)
    {
        _model = model;
        Key = model.Key;
        Value = model.Value;
        Description = model.Description;
        IsActive = model.IsActive;
    }

    public ParamItem ToModel()
    {
        _model.Key = Key;
        _model.Value = Value;
        _model.Description = Description;
        _model.IsActive = IsActive;
        return _model;
    }
}

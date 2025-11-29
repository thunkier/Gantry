using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.Core.Domain.Settings;

namespace Gantry.UI.Features.Requests.ViewModels;

public partial class VariableViewModel : ObservableObject
{
    public Variable Model { get; }

    public VariableViewModel(Variable model)
    {
        Model = model;
    }

    public string Key
    {
        get => Model.Key;
        set
        {
            if (Model.Key != value)
            {
                Model.Key = value;
                OnPropertyChanged();
            }
        }
    }

    public string Value
    {
        get => Model.Value;
        set
        {
            if (Model.Value != value)
            {
                Model.Value = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Enabled
    {
        get => Model.Enabled;
        set
        {
            if (Model.Enabled != value)
            {
                Model.Enabled = value;
                OnPropertyChanged();
            }
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.Core.Domain.Settings;

namespace Gantry.UI.Features.Requests.ViewModels;

public partial class AuthSettingsViewModel : ObservableObject
{
    private readonly AuthSettings _model;

    public AuthSettingsViewModel(AuthSettings model)
    {
        _model = model;
    }

    public AuthType Type
    {
        get => _model.Type;
        set
        {
            if (_model.Type != value)
            {
                _model.Type = value;
                OnPropertyChanged();
            }
        }
    }

    public string Token
    {
        get => _model.Token;
        set
        {
            if (_model.Token != value)
            {
                _model.Token = value;
                OnPropertyChanged();
            }
        }
    }

    public string Username
    {
        get => _model.Username;
        set
        {
            if (_model.Username != value)
            {
                _model.Username = value;
                OnPropertyChanged();
            }
        }
    }

    public string Password
    {
        get => _model.Password;
        set
        {
            if (_model.Password != value)
            {
                _model.Password = value;
                OnPropertyChanged();
            }
        }
    }
}

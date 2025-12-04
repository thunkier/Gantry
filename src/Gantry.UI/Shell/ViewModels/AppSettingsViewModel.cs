using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gantry.Core.Domain.Settings;
using Gantry.Infrastructure.Services;
using Gantry.UI.Features.Requests.ViewModels;

namespace Gantry.UI.Shell.ViewModels;

public partial class AppSettingsViewModel : ObservableObject
{
    private readonly SystemVariableService _systemVariableService;

    public ObservableCollection<VariableViewModel> SystemVariables { get; } = new();

    // 1. Add a property to hold the Request Settings
    public RequestViewModel RequestSettings { get; }

    public AppSettingsViewModel()
    {
        // 2. Initialize the Request Settings
        // In a real app, you might load these defaults from a service/file
        RequestSettings = new RequestViewModel(); 

        _systemVariableService = new SystemVariableService();
        foreach (var v in _systemVariableService.Variables)
        {
            var vm = new VariableViewModel(v);
            vm.PropertyChanged += (s, e) => Save();
            SystemVariables.Add(vm);
        }
    }

    [RelayCommand]
    private void AddVariable()
    {
        var v = new Variable { Key = "NewVar", Value = "Value", Enabled = true };
        _systemVariableService.Variables.Add(v);
        
        var vm = new VariableViewModel(v);
        vm.PropertyChanged += (s, e) => Save();
        SystemVariables.Add(vm);
        Save();
    }

    [RelayCommand]
    private void RemoveVariable(VariableViewModel vm)
    {
        if (vm == null) return;
        _systemVariableService.Variables.Remove(vm.Model);
        SystemVariables.Remove(vm);
        Save();
    }

    private void Save()
    {
        _systemVariableService.Save();
    }
}
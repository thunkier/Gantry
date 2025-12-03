using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.Core.Domain.Collections;
using Gantry.UI.Features.Requests.ViewModels;
using Gantry.UI.Interfaces;
using System.Collections.ObjectModel;

namespace Gantry.UI.Features.Collections.ViewModels;

public partial class CollectionViewModel : ObservableObject, ITreeItemViewModel
{
    public Collection Model { get; }

    private readonly Gantry.Infrastructure.Services.WorkspaceService _workspaceService;

    public CollectionViewModel(Collection model, Gantry.Infrastructure.Services.WorkspaceService workspaceService)
    {
        Model = model;
        _workspaceService = workspaceService;

        foreach (var sub in model.SubCollections)
        {
            Children.Add(new CollectionViewModel(sub, _workspaceService));
        }

        foreach (var req in model.Requests)
        {
            Children.Add(new RequestItemViewModel(req));
        }

        foreach (var graph in model.NodeGraphs)
        {
            Children.Add(new NodeTaskViewModel(graph, _workspaceService));
        }

        Auth = new AuthSettingsViewModel(model.Auth);
        foreach (var v in model.Variables)
        {
            var vm = new VariableViewModel(v);
            vm.PropertyChanged += Variable_PropertyChanged;
            Variables.Add(vm);
        }

        if (Variables.Count == 0)
        {
            AddVariable();
        }
        else if (!string.IsNullOrEmpty(Variables.Last().Key) || !string.IsNullOrEmpty(Variables.Last().Value))
        {
            AddVariable();
        }
    }

    public string Name => Model.Name;

    public bool IsRoot { get; set; }

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editableName = string.Empty;

    // Placeholders for the collection tabs
    // Settings
    public AuthSettingsViewModel Auth { get; }
    public Gantry.Core.Domain.Settings.ScriptSettings Scripts => Model.Scripts;
    public ObservableCollection<VariableViewModel> Variables { get; } = [];

    // Helper for Auth Types
    public static System.Collections.Generic.IEnumerable<Gantry.Core.Domain.Settings.AuthType> AuthTypes =>
        System.Enum.GetValues<Gantry.Core.Domain.Settings.AuthType>();

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void AddVariable()
    {
        var variable = new Gantry.Core.Domain.Settings.Variable { Key = "", Value = "" };
        var vm = new VariableViewModel(variable);
        vm.PropertyChanged += Variable_PropertyChanged;
        Model.Variables.Add(variable);
        Variables.Add(vm);
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void RemoveVariable(VariableViewModel variable)
    {
        if (variable != null)
        {
            variable.PropertyChanged -= Variable_PropertyChanged;
            Model.Variables.Remove(variable.Model);
            Variables.Remove(variable);

            // Ensure there's always at least one empty row
            if (Variables.Count == 0)
            {
                AddVariable();
            }
        }
    }

    private void Variable_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is VariableViewModel vm && Variables.LastOrDefault() == vm)
        {
            if (!string.IsNullOrEmpty(vm.Key) || !string.IsNullOrEmpty(vm.Value))
            {
                AddVariable();
            }
        }
    }

    public ObservableCollection<ITreeItemViewModel> Children { get; } = [];

    public void RaiseNameChanged()
    {
        OnPropertyChanged(nameof(Name));
    }
}

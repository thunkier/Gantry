using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.UI.Features.NodeEditor.ViewModels;

namespace Gantry.UI.Features.Collections.ViewModels;

public partial class NodeTaskViewModel : ObservableObject, Gantry.UI.Interfaces.ITreeItemViewModel
{
    [ObservableProperty]
    private string _name;

    public NodeEditorViewModel NodeEditor { get; }

    // ITreeItemViewModel implementation
    public System.Collections.ObjectModel.ObservableCollection<Gantry.UI.Interfaces.ITreeItemViewModel> Children { get; } = new();

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editableName = string.Empty;

    public void RaiseNameChanged()
    {
        OnPropertyChanged(nameof(Name));
    }

    public NodeTaskViewModel(Gantry.Core.Domain.NodeEditor.NodeGraph graph, Gantry.Infrastructure.Services.WorkspaceService workspaceService, System.Func<System.Threading.Tasks.Task<Gantry.Core.Domain.Collections.RequestItem?>>? requestSelector = null)
    {
        Name = graph.Name;
        NodeEditor = new NodeEditorViewModel(graph, workspaceService)
        {
            RequestSelector = requestSelector
        };
    }
}

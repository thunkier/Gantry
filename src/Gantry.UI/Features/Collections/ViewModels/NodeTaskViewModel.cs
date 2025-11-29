using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.UI.Features.NodeEditor.ViewModels;

namespace Gantry.UI.Features.Collections.ViewModels;

public partial class NodeTaskViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;

    public NodeEditorViewModel NodeEditor { get; }

    public NodeTaskViewModel(Gantry.Core.Domain.NodeEditor.NodeGraph graph, Gantry.Infrastructure.Services.WorkspaceService workspaceService, System.Func<System.Threading.Tasks.Task<Gantry.Core.Domain.Collections.RequestItem?>>? requestSelector = null)
    {
        Name = graph.Name;
        NodeEditor = new NodeEditorViewModel(graph, workspaceService)
        {
            RequestSelector = requestSelector
        };
    }
}

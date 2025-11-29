using Gantry.UI.Features.Collections.ViewModels;
using Gantry.UI.Features.NodeEditor.ViewModels;

namespace Gantry.UI.Shell.ViewModels;

public class NodeEditorTabViewModel : TabViewModel
{
    public NodeTaskViewModel NodeTask { get; }
    public NodeEditorViewModel NodeEditor => NodeTask.NodeEditor;

    public NodeEditorTabViewModel(NodeTaskViewModel nodeTask)
    {
        NodeTask = nodeTask;
        Title = nodeTask.Name;

        // Listen for name changes if necessary, or bind directly in XAML
        nodeTask.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NodeTask.Name))
            {
                Title = NodeTask.Name;
            }
        };
    }
}

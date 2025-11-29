using Gantry.UI.Features.NodeEditor.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gantry.UI.Features.NodeEditor.Services;

public class GraphRunner
{
    public async Task RunGraphAsync(IEnumerable<NodeViewModel> nodes, IEnumerable<ConnectionViewModel> connections)
    {
        var sortedNodes = TopologicalSort(nodes, connections);

        foreach (var node in sortedNodes)
        {
            if (node is RequestNodeViewModel requestNode)
            {
                await ExecuteRequestNode(requestNode);
            }
        }
    }

    private List<NodeViewModel> TopologicalSort(IEnumerable<NodeViewModel> nodes, IEnumerable<ConnectionViewModel> connections)
    {
        var sorted = new List<NodeViewModel>();
        var visited = new HashSet<NodeViewModel>();
        var visiting = new HashSet<NodeViewModel>();

        foreach (var node in nodes)
        {
            Visit(node, visited, visiting, sorted, connections);
        }

        sorted.Reverse(); // Depends on implementation, but usually reverse post-order
        return sorted;
    }

    private void Visit(NodeViewModel node, HashSet<NodeViewModel> visited, HashSet<NodeViewModel> visiting, List<NodeViewModel> sorted, IEnumerable<ConnectionViewModel> connections)
    {
        if (visited.Contains(node)) return;
        if (visiting.Contains(node)) throw new System.Exception("Cycle detected");

        visiting.Add(node);

        // Find dependencies (nodes that output to this node's inputs)
        var dependencies = connections
            .Where(c => c.Target.Parent == node)
            .Select(c => c.Source.Parent);

        foreach (var dep in dependencies)
        {
            Visit(dep, visited, visiting, sorted, connections);
        }

        visiting.Remove(node);
        visited.Add(node);
        sorted.Add(node);
    }

    private async Task ExecuteRequestNode(RequestNodeViewModel node)
    {
        // Placeholder for actual execution logic
        // In a real implementation, we would use an HttpClient service here
        System.Diagnostics.Debug.WriteLine($"Executing Request: {node.Title}");
        await Task.Delay(500); // Simulate network delay
        System.Diagnostics.Debug.WriteLine($"Finished Request: {node.Title}");
    }
}

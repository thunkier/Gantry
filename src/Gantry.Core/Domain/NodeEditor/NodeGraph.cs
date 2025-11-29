using System.Collections.Generic;
using Gantry.Core.Domain.Collections;

namespace Gantry.Core.Domain.NodeEditor;

public class NodeGraph : Gantry.Core.Domain.Settings.ISettingsContainer
{
    public string Name { get; set; } = "New Graph";
    public string Path { get; set; } = string.Empty;
    public Gantry.Core.Domain.Settings.ISettingsContainer? Parent { get; set; }

    public Gantry.Core.Domain.Settings.AuthSettings Auth { get; set; } = new();
    public Gantry.Core.Domain.Settings.ScriptSettings Scripts { get; set; } = new();
    public System.Collections.ObjectModel.ObservableCollection<Gantry.Core.Domain.Collections.HeaderItem> Headers { get; set; } = new();
    public System.Collections.ObjectModel.ObservableCollection<Gantry.Core.Domain.Settings.Variable> Variables { get; set; } = new();

    public List<NodeModel> Nodes { get; set; } = new();
    public List<ConnectionModel> Connections { get; set; } = new();
}

public class NodeModel
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Type { get; set; } = "RequestNode"; // Discriminator
    public double X { get; set; }
    public double Y { get; set; }

    // For RequestNode
    public RequestItem? RequestItem { get; set; }
}

public class ConnectionModel
{
    public string SourceNodeId { get; set; } = string.Empty;
    public string SourcePinId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string TargetPinId { get; set; } = string.Empty;
}

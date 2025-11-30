using Gantry.Core.Domain.Settings;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Gantry.Core.Domain.Collections;

public class Collection : ISettingsContainer
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PostmanId { get; set; } = string.Empty;
    public string SchemaUrl { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ObservableCollection<Collection> SubCollections { get; set; } = new();
    public ObservableCollection<RequestItem> Requests { get; set; } = new();
    public ObservableCollection<Gantry.Core.Domain.NodeEditor.NodeGraph> NodeGraphs { get; set; } = new();

    // ISettingsContainer implementation
    [JsonIgnore]
    public ISettingsContainer? Parent { get; set; }
    public AuthSettings Auth { get; set; } = new();
    public ScriptSettings Scripts { get; set; } = new();
    public ObservableCollection<HeaderItem> Headers { get; set; } = new();
    public ObservableCollection<Variable> Variables { get; set; } = new();
}

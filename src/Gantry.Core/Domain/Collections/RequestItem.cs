using Gantry.Core.Domain.Http;
using Gantry.Core.Domain.Settings;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Gantry.Core.Domain.Collections;

public class RequestItem : ISettingsContainer
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RequestModel Request { get; set; } = new();

    // ISettingsContainer implementation
    [JsonIgnore]
    public ISettingsContainer? Parent { get; set; }
    public AuthSettings Auth { get; set; } = new();
    public ScriptSettings Scripts { get; set; } = new();
    public ObservableCollection<Variable> Variables { get; set; } = new();
    public ObservableCollection<ParamItem> Params { get; set; } = new();
    public ObservableCollection<HeaderItem> Headers { get; set; } = new();
    public ObservableCollection<RequestHistoryItem> History { get; set; } = new();
}

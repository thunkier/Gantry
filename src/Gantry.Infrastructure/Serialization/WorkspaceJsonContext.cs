using System.Text.Json.Serialization;
using Gantry.Core.Domain.Workspaces;
using System.Collections.Generic;
using Gantry.Core.Domain.Settings;

namespace Gantry.Infrastructure.Serialization;

[JsonSerializable(typeof(List<Workspace>))]
[JsonSerializable(typeof(Workspace))]
[JsonSerializable(typeof(Gantry.Core.Domain.Collections.Collection))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(AppSessionState))]
[JsonSerializable(typeof(Gantry.Core.Domain.Http.SavedResponse))]
[JsonSerializable(typeof(List<Gantry.Core.Domain.Http.SavedResponse>))]
[JsonSerializable(typeof(Gantry.Core.Domain.NodeEditor.NodeGraph))]
[JsonSerializable(typeof(Gantry.Core.Domain.NodeEditor.NodeModel))]
[JsonSerializable(typeof(Gantry.Core.Domain.NodeEditor.ConnectionModel))]
[JsonSerializable(typeof(Gantry.Core.Domain.Collections.RequestItem))]
[JsonSerializable(typeof(Gantry.Core.Domain.Http.RequestModel))]
[JsonSerializable(typeof(Gantry.Core.Domain.Http.AuthConfig))]
[JsonSerializable(typeof(Gantry.Core.Domain.Http.ProxyConfig))]
[JsonSerializable(typeof(Gantry.Core.Domain.Http.CertificateConfig))]
[JsonSerializable(typeof(Gantry.Core.Domain.Http.HttpVersionPolicy))]
[JsonSerializable(typeof(Gantry.Core.Domain.Settings.AuthSettings))]
[JsonSerializable(typeof(Gantry.Core.Domain.Settings.ScriptSettings))]
[JsonSerializable(typeof(Gantry.Core.Domain.Settings.Variable))]
[JsonSerializable(typeof(Gantry.Core.Domain.Collections.HeaderItem))]
[JsonSerializable(typeof(Gantry.Core.Domain.Collections.ParamItem))]
[JsonSerializable(typeof(Gantry.Core.Domain.Collections.RequestHistoryItem))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<Gantry.Core.Domain.NodeEditor.NodeModel>))]
[JsonSerializable(typeof(List<Gantry.Core.Domain.NodeEditor.ConnectionModel>))]
[JsonSerializable(typeof(System.Collections.ObjectModel.ObservableCollection<Gantry.Core.Domain.NodeEditor.NodeGraph>))]
[JsonSerializable(typeof(System.Collections.ObjectModel.ObservableCollection<Gantry.Core.Domain.Collections.RequestItem>))]
[JsonSerializable(typeof(System.Collections.ObjectModel.ObservableCollection<Gantry.Core.Domain.Settings.Variable>))]
[JsonSerializable(typeof(System.Collections.ObjectModel.ObservableCollection<Gantry.Core.Domain.Collections.HeaderItem>))]
[JsonSerializable(typeof(System.Collections.ObjectModel.ObservableCollection<Gantry.Core.Domain.Collections.ParamItem>))]
[JsonSerializable(typeof(System.Collections.ObjectModel.ObservableCollection<Gantry.Core.Domain.Collections.RequestHistoryItem>))]
public partial class WorkspaceJsonContext : JsonSerializerContext
{
}

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
public partial class WorkspaceJsonContext : JsonSerializerContext
{
}

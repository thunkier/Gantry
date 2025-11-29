using System.Collections.ObjectModel;

namespace Gantry.Core.Domain.Settings;

public interface ISettingsContainer
{
    ISettingsContainer? Parent { get; set; }
    string Name { get; set; }
    string Path { get; set; }
    AuthSettings Auth { get; set; }
    ScriptSettings Scripts { get; set; }
    ObservableCollection<Gantry.Core.Domain.Collections.HeaderItem> Headers { get; set; }
    ObservableCollection<Variable> Variables { get; set; }
}

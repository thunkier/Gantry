using Gantry.Core.Domain.Settings;

namespace Gantry.Core.Interfaces;

public interface IVariableService
{
    string ResolveVariables(string input, ISettingsContainer context);
}

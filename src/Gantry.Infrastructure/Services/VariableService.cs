using System.Text.RegularExpressions;
using Gantry.Core.Domain.Settings;
using Gantry.Core.Interfaces;

namespace Gantry.Infrastructure.Services;

public class VariableService : IVariableService
{
    // Matches ${variableName}
    private static readonly Regex VariableRegex = new(@"\$\{(.+?)\}", RegexOptions.Compiled);

    public string ResolveVariables(string input, ISettingsContainer context)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return VariableRegex.Replace(input, match =>
        {
            var variableName = match.Groups[1].Value;
            var value = FindVariableValue(variableName, context);
            return value ?? match.Value; // Return original if not found
        });
    }

    private string? FindVariableValue(string key, ISettingsContainer? context)
    {
        var current = context;
        while (current != null)
        {
            var variable = current.Variables.FirstOrDefault(v => v.Enabled && v.Key == key);
            if (variable != null)
            {
                return variable.Value;
            }
            current = current.Parent;
        }
        return null;
    }
}

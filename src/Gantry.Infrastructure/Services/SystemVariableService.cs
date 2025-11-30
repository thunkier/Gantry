using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gantry.Core.Domain.Settings;
using Tomlyn;
using Tomlyn.Model;

namespace Gantry.Infrastructure.Services;

public class SystemVariableService
{
    private readonly string _path;
    public List<Variable> Variables { get; private set; } = new();

    public SystemVariableService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var gantryDir = Path.Combine(appData, "Gantry");
        if (!Directory.Exists(gantryDir)) Directory.CreateDirectory(gantryDir);
        _path = Path.Combine(gantryDir, "user.toml");
        Load();
    }

    public void Load()
    {
        Variables.Clear();
        if (!File.Exists(_path)) return;

        try
        {
            var toml = File.ReadAllText(_path);
            var model = Toml.ToModel(toml);
            
            // Assuming simple key-value pairs at root or under [variables]
            // Let's support both for flexibility, but prioritize [variables] table if exists
            // Actually, user.toml usually has keys at root for simple config, but let's stick to a structure.
            // If the user just puts `apiKey = "secret"`, it's at the root.
            
            foreach (var kvp in model)
            {
                if (kvp.Value is string s)
                {
                    Variables.Add(new Variable { Key = kvp.Key, Value = s, Enabled = true });
                }
                else if (kvp.Key == "variables" && kvp.Value is TomlTable tbl)
                {
                    foreach (var v in tbl)
                    {
                        Variables.Add(new Variable { Key = v.Key, Value = v.Value?.ToString() ?? "", Enabled = true });
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    public void Save()
    {
        var sb = new StringBuilder();
        foreach (var v in Variables.Where(v => v.Enabled && !string.IsNullOrWhiteSpace(v.Key)))
        {
            sb.AppendLine($"{v.Key} = \"{v.Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
        }
        File.WriteAllText(_path, sb.ToString());
    }
}

using Gantry.Core.Domain.Collections;
using Gantry.Core.Domain.Http;
using Gantry.Core.Domain.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace Gantry.Infrastructure.Persistence;

public class RequestBundleRepository
{
    public RequestItem LoadBundle(string bundlePath, ISettingsContainer? parent)
    {
        var dirInfo = new DirectoryInfo(bundlePath);
        var requestItem = new RequestItem
        {
            Name = dirInfo.Name.Replace(".req", ""),
            Path = bundlePath,
            Parent = parent
        };

        // 1. Load meta.toml
        var metaPath = Path.Combine(bundlePath, "meta.toml");
        if (File.Exists(metaPath))
        {
            var toml = File.ReadAllText(metaPath);
            var model = Toml.ToModel(toml);

            if (model.TryGetValue("name", out var name)) requestItem.Name = (string)name;
            if (model.TryGetValue("url", out var url)) requestItem.Request.Url = (string)url;
            if (model.TryGetValue("method", out var method)) requestItem.Request.Method = (string)method;

            // Headers
            if (model.TryGetValue("headers", out var headersObj) && headersObj is TomlTable headersTable)
            {
                foreach (var kvp in headersTable)
                {
                    requestItem.Headers.Add(new HeaderItem { Key = kvp.Key, Value = kvp.Value?.ToString() ?? "", IsActive = true });
                }
            }

            // Params
            if (model.TryGetValue("params", out var paramsObj) && paramsObj is TomlTable paramsTable)
            {
                foreach (var kvp in paramsTable)
                {
                    requestItem.Params.Add(new ParamItem { Key = kvp.Key, Value = kvp.Value?.ToString() ?? "", IsActive = true });
                }
            }

            // Auth
            if (model.TryGetValue("auth", out var authObj) && authObj is TomlTable authTable)
            {
                if (authTable.TryGetValue("type", out var typeStr) && Enum.TryParse<AuthType>((string)typeStr, true, out var type))
                {
                    requestItem.Auth.Type = type;
                }
                if (authTable.TryGetValue("username", out var username)) requestItem.Auth.Username = (string)username;
                if (authTable.TryGetValue("password", out var password)) requestItem.Auth.Password = (string)password;
                if (authTable.TryGetValue("token", out var token)) requestItem.Auth.Token = (string)token;
            }
        }

        // 1.1 Load Description (readme.md)
        var readmePath = Path.Combine(bundlePath, "readme.md");
        if (File.Exists(readmePath))
        {
            requestItem.Description = File.ReadAllText(readmePath);
        }

        // 2. Load Body
        var bodyPath = Path.Combine(bundlePath, "body.json");
        if (File.Exists(bodyPath))
        {
            requestItem.Request.Body = File.ReadAllText(bodyPath);
        }
        else
        {
            // Try other extensions
            var txtPath = Path.Combine(bundlePath, "body.txt");
            if (File.Exists(txtPath)) requestItem.Request.Body = File.ReadAllText(txtPath);

            var xmlPath = Path.Combine(bundlePath, "body.xml");
            if (File.Exists(xmlPath)) requestItem.Request.Body = File.ReadAllText(xmlPath);
        }

        // 3. Load Scripts
        var preScriptPath = Path.Combine(bundlePath, "pre-script.js");
        if (File.Exists(preScriptPath)) requestItem.Scripts.PreRequestScript = File.ReadAllText(preScriptPath);

        // Support both test.js (new standard) and post-script.js (legacy)
        var testScriptPath = Path.Combine(bundlePath, "test.js");
        if (File.Exists(testScriptPath))
        {
            requestItem.Scripts.PostResponseScript = File.ReadAllText(testScriptPath);
        }
        else
        {
            var postScriptPath = Path.Combine(bundlePath, "post-script.js");
            if (File.Exists(postScriptPath)) requestItem.Scripts.PostResponseScript = File.ReadAllText(postScriptPath);
        }

        return requestItem;
    }

    public void SaveBundle(RequestItem item)
    {
        // Ensure directory exists (it might be a new request or renamed)
        // If the item path doesn't end in .req, we might need to adjust it or create a new folder
        // For now, assume item.Path is the folder path ending in .req

        if (!item.Path.EndsWith(".req"))
        {
            // If it's a file path (legacy), we need to handle migration or error
            // Assuming for new items it's set correctly by the UI/Service
        }

        if (!Directory.Exists(item.Path))
        {
            Directory.CreateDirectory(item.Path);
        }

        // 1. Save meta.toml
        var sb = new StringBuilder();
        sb.AppendLine($"name = \"{EscapeToml(item.Name)}\"");
        sb.AppendLine($"url = \"{EscapeToml(item.Request.Url)}\"");
        sb.AppendLine($"method = \"{EscapeToml(item.Request.Method)}\"");
        sb.AppendLine();

        if (item.Headers.Any())
        {
            sb.AppendLine("[headers]");
            foreach (var h in item.Headers.Where(h => h.IsActive && !string.IsNullOrEmpty(h.Key)))
            {
                sb.AppendLine($"{h.Key} = \"{EscapeToml(h.Value)}\"");
            }
            sb.AppendLine();
        }

        if (item.Params.Any())
        {
            sb.AppendLine("[params]");
            foreach (var p in item.Params.Where(p => p.IsActive && !string.IsNullOrEmpty(p.Key)))
            {
                sb.AppendLine($"{p.Key} = \"{EscapeToml(p.Value)}\"");
            }
            sb.AppendLine();
        }

        if (item.Auth.Type != AuthType.None)
        {
            sb.AppendLine("[auth]");
            sb.AppendLine($"type = \"{item.Auth.Type.ToString().ToLower()}\"");
            if (!string.IsNullOrEmpty(item.Auth.Username)) sb.AppendLine($"username = \"{EscapeToml(item.Auth.Username)}\"");
            if (!string.IsNullOrEmpty(item.Auth.Password)) sb.AppendLine($"password = \"{EscapeToml(item.Auth.Password)}\"");
            if (!string.IsNullOrEmpty(item.Auth.Token)) sb.AppendLine($"token = \"{EscapeToml(item.Auth.Token)}\"");
        }

        File.WriteAllText(Path.Combine(item.Path, "meta.toml"), sb.ToString());

        // 2. Save Body
        // Determine extension based on content? For now default to .json or .txt
        // Or check if body.json exists vs body.xml
        var bodyFile = "body.json"; // Default
        // Logic to detect type could go here
        File.WriteAllText(Path.Combine(item.Path, bodyFile), item.Request.Body ?? "");

        // 3. Save Scripts (Clean Bundle Logic)
        var preScriptPath = Path.Combine(item.Path, "pre-script.js");
        if (!string.IsNullOrWhiteSpace(item.Scripts.PreRequestScript))
        {
            File.WriteAllText(preScriptPath, item.Scripts.PreRequestScript);
        }
        else if (File.Exists(preScriptPath))
        {
            File.Delete(preScriptPath);
        }

        var testScriptPath = Path.Combine(item.Path, "test.js");
        var legacyPostScriptPath = Path.Combine(item.Path, "post-script.js");

        if (!string.IsNullOrWhiteSpace(item.Scripts.PostResponseScript))
        {
            File.WriteAllText(testScriptPath, item.Scripts.PostResponseScript);
            // Clean up legacy file if it exists
            if (File.Exists(legacyPostScriptPath)) File.Delete(legacyPostScriptPath);
        }
        else
        {
            if (File.Exists(testScriptPath)) File.Delete(testScriptPath);
            if (File.Exists(legacyPostScriptPath)) File.Delete(legacyPostScriptPath);
        }

        // 4. Save Description (Clean Bundle Logic)
        var readmePath = Path.Combine(item.Path, "readme.md");
        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            File.WriteAllText(readmePath, item.Description);
        }
        else if (File.Exists(readmePath))
        {
            File.Delete(readmePath);
        }
    }

    private string EscapeToml(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

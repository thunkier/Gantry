using System.Text;
using System.Text.Json;
using Gantry.Core.Domain.Collections;
using Gantry.Core.Domain.NodeEditor;
using Gantry.Core.Domain.Settings;
using Gantry.Infrastructure.Serialization;
using Tomlyn;
using Tomlyn.Model;

namespace Gantry.Infrastructure.Persistence;

public class FileSystemCollectionRepository
{
    private readonly JsonSerializerOptions _opts = new() { WriteIndented = true, TypeInfoResolver = WorkspaceJsonContext.Default };
    private readonly RequestBundleRepository _bundles = new();

    public Collection LoadCollection(string path, ISettingsContainer? parent = null)
    {
        // 1. Single File
        if (File.Exists(path))
            return TryLoadJson<Collection>(path, s => s.Contains("\"Requests\"") || s.Contains("\"SubCollections\""),
                c => { c.Parent = parent; FixParents(c); }) ?? new Collection { Name = Path.GetFileName(path), Path = path, Parent = parent };

        // 2. Directory
        var c = new Collection { Name = new DirectoryInfo(path).Name, Path = path, Parent = parent };
        LoadMeta(c, Path.Combine(path, "meta.toml"));

        if (!Directory.Exists(path)) return c;

        foreach (var d in Directory.GetDirectories(path))
        {
            if (d.EndsWith(".req", StringComparison.OrdinalIgnoreCase)) c.Requests.Add(_bundles.LoadBundle(d, c));
            else c.SubCollections.Add(LoadCollection(d, c));
        }

        foreach (var f in Directory.GetFiles(path).Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".gantry", StringComparison.OrdinalIgnoreCase)))
        {
            if (TryLoadJson<Collection>(f, _ => true, sub => { sub.Parent = c; FixParents(sub); }) is { } sub) { c.SubCollections.Add(sub); continue; }
            if (TryLoadJson<RequestItem>(f, _ => true, req => req.Parent = c) is { } req) { c.Requests.Add(req); continue; }
            if (TryLoadJson<NodeGraph>(f, s => s.Contains("\"Nodes\""), g => g.Parent = c) is { } graph) { c.NodeGraphs.Add(graph); }
        }
        return c;
    }

    public void SaveRequest(RequestItem item)
    {
        var root = GetRoot(item);
        if (IsSingleFile(root)) { SaveCollection(root!); return; }

        if (!item.Path.EndsWith(".req")) item.Path = Path.Combine(Path.GetDirectoryName(item.Path) ?? "", $"{Sanitize(item.Name)}.req");
        _bundles.SaveBundle(item);
    }

    public void SaveNodeGraph(NodeGraph g) => WriteJson(g, Path.ChangeExtension(g.Path, ".json"));

    public void SaveCollection(Collection c)
    {
        if (IsSingleFile(c)) { WriteJson(c, c.Path); return; }

        Directory.CreateDirectory(c.Path);
        SaveMeta(c, Path.Combine(c.Path, "meta.toml"));

        void EnsurePath(ISettingsContainer i, string ext) =>
            i.Path = (!string.IsNullOrEmpty(i.Path) && Path.IsPathRooted(i.Path)) ? i.Path : Path.Combine(c.Path, $"{Sanitize(i.Name)}{ext}");

        foreach (var sub in c.SubCollections) { EnsurePath(sub, ""); SaveCollection(sub); }
        foreach (var req in c.Requests) { EnsurePath(req, ".req"); SaveRequest(req); }
        foreach (var grp in c.NodeGraphs) { EnsurePath(grp, ".json"); SaveNodeGraph(grp); }
    }

    // --- Helpers ---

    private T? TryLoadJson<T>(string path, Func<string, bool> validate, Action<T>? post = null)
    {
        try
        {
            var json = File.ReadAllText(path);
            if (!validate(json)) return default;
            var obj = JsonSerializer.Deserialize<T>(json, _opts);
            if (obj is ISettingsContainer sc) sc.Path = path;
            if (obj != null) { post?.Invoke(obj); return obj; }
        }
        catch { /* Ignore */ }
        return default;
    }

    private void WriteJson<T>(T obj, string path)
    {
        if (string.IsNullOrEmpty(Path.GetDirectoryName(path))) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(obj, _opts));
    }

    private void LoadMeta(Collection c, string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var m = Toml.ToModel(File.ReadAllText(path));
            if (m.TryGetValue("variables", out var vObj) && vObj is TomlTable vTbl)
                foreach (var k in vTbl) c.Variables.Add(new Variable { Key = k.Key, Value = k.Value?.ToString() ?? "", Enabled = true });

            if (m.TryGetValue("auth", out var aObj) && aObj is TomlTable aTbl)
            {
                if (aTbl.TryGetValue("type", out var t) && Enum.TryParse<AuthType>(t?.ToString(), true, out var type)) c.Auth.Type = type;
                if (aTbl.TryGetValue("username", out var u)) c.Auth.Username = (string)u;
                if (aTbl.TryGetValue("password", out var p)) c.Auth.Password = (string)p;
                if (aTbl.TryGetValue("token", out var tk)) c.Auth.Token = (string)tk;
            }
        }
        catch { }
    }

    private void SaveMeta(Collection c, string path)
    {
        var sb = new StringBuilder();
        if (c.Variables.Any())
        {
            sb.AppendLine("[variables]");
            foreach (var v in c.Variables.Where(v => v.Enabled && !string.IsNullOrEmpty(v.Key)))
                sb.AppendLine($"{v.Key} = \"{Esc(v.Value)}\"");
        }
        if (c.Auth.Type != AuthType.None)
        {
            sb.AppendLine($"\n[auth]\ntype = \"{c.Auth.Type.ToString().ToLower()}\"");
            if (!string.IsNullOrEmpty(c.Auth.Username)) sb.AppendLine($"username = \"{Esc(c.Auth.Username)}\"");
            if (!string.IsNullOrEmpty(c.Auth.Password)) sb.AppendLine($"password = \"{Esc(c.Auth.Password)}\"");
            if (!string.IsNullOrEmpty(c.Auth.Token)) sb.AppendLine($"token = \"{Esc(c.Auth.Token)}\"");
        }
        if (sb.Length > 0) File.WriteAllText(path, sb.ToString());
    }

    private void FixParents(Collection c)
    {
        foreach (var s in c.SubCollections) { s.Parent = c; FixParents(s); }
        foreach (var r in c.Requests) r.Parent = c;
    }

    private Collection? GetRoot(ISettingsContainer? i) => i?.Parent == null ? i as Collection : GetRoot(i.Parent);
    private bool IsSingleFile(Collection? c) => c != null && (File.Exists(c.Path) || c.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || c.Path.EndsWith(".gantry", StringComparison.OrdinalIgnoreCase));
    private string Sanitize(string n) => string.Join("_", n.Split(Path.GetInvalidFileNameChars()));
    private string Esc(string v) => v.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
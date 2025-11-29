using Gantry.Core.Domain.Collections;
using Gantry.Core.Domain.Http;
using Gantry.Core.Domain.Settings;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Gantry.Infrastructure.Export;

public class BrunoExporter : ICollectionExporter
{
    public string Name => "Bruno";
    public static string Extension => "zip";

    private const string Indent = "  ";

    public async Task<byte[]> ExportAsync(Collection collection)
    {
        using var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // 1. Create root configuration (bruno.json)
            CreateBrunoConfig(collection, archive);

            // 2. Process root requests
            ProcessRequests(collection.Requests, "", archive);

            // 3. Process sub-collections recursively
            ProcessFolders(collection.SubCollections, "", archive);
        }

        return memoryStream.ToArray();
    }

    private void CreateBrunoConfig(Collection collection, ZipArchive archive)
    {
        var entry = archive.CreateEntry("bruno.json");
        using var stream = entry.Open();

        var config = new
        {
            version = "1",
            name = collection.Name,
            type = "collection",
            ignore = new[] { ".git", "node_modules" }
        };

        JsonSerializer.Serialize(stream, config, new JsonSerializerOptions { WriteIndented = true });
    }

    private void ProcessFolders(IEnumerable<Collection> folders, string parentPath, ZipArchive archive)
    {
        foreach (var folder in folders)
        {
            // Bruno uses folders for structure; we just append the path
            // Normalizing path separators to forward slash for zip compatibility
            var currentPath = string.IsNullOrEmpty(parentPath)
                ? folder.Name
                : $"{parentPath}/{folder.Name}";

            ProcessRequests(folder.Requests, currentPath, archive);
            ProcessFolders(folder.SubCollections, currentPath, archive);
        }
    }

    private void ProcessRequests(IEnumerable<RequestItem> requests, string path, ZipArchive archive)
    {
        int sequence = 1;
        foreach (var item in requests)
        {
            var fileName = $"{item.Name}.bru";
            var entryPath = string.IsNullOrEmpty(path) ? fileName : $"{path}/{fileName}";

            var entry = archive.CreateEntry(entryPath);
            using var writer = new StreamWriter(entry.Open());

            writer.Write(GenerateBruContent(item, sequence++));
        }
    }

    private string GenerateBruContent(RequestItem item, int sequence)
    {
        var sb = new StringBuilder();
        var req = item.Request;

        // --- Meta Block ---
        sb.AppendLine("meta {");
        sb.AppendLine($"{Indent}name: {item.Name}");
        sb.AppendLine($"{Indent}type: http");
        sb.AppendLine($"{Indent}seq: {sequence}");
        sb.AppendLine("}");
        sb.AppendLine();

        // --- HTTP Block ---
        var method = req.Method.ToLower();
        var bodyType = InferBodyType(req);
        var authType = MapAuthType(item.Auth.Type);

        sb.AppendLine($"{method} {{");
        sb.AppendLine($"{Indent}url: {req.Url}");
        sb.AppendLine($"{Indent}body: {bodyType}");
        sb.AppendLine($"{Indent}auth: {authType}");
        sb.AppendLine("}");
        sb.AppendLine();

        // --- Headers ---
        if (req.Headers?.Count > 0)
        {
            sb.AppendLine("headers {");
            foreach (var header in req.Headers)
            {
                sb.AppendLine($"{Indent}{header.Key}: {header.Value}");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // --- Authentication ---
        if (authType != "none" && authType != "inherit")
        {
            WriteAuthBlock(sb, item.Auth);
        }

        // --- Body ---
        if (bodyType != "none" && !string.IsNullOrWhiteSpace(req.Body))
        {
            // Bruno uses "body:json", "body:text", etc.
            sb.AppendLine($"body:{bodyType} {{");
            sb.AppendLine(req.Body);
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // --- Scripts (Basic mapping) ---
        if (!string.IsNullOrWhiteSpace(item.Scripts.PreRequestScript))
        {
            sb.AppendLine("script:pre-request {");
            sb.AppendLine(item.Scripts.PreRequestScript);
            sb.AppendLine("}");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(item.Scripts.PostResponseScript))
        {
            sb.AppendLine("script:post-response {");
            sb.AppendLine(item.Scripts.PostResponseScript);
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void WriteAuthBlock(StringBuilder sb, AuthSettings auth)
    {
        switch (auth.Type)
        {
            case AuthType.BearerToken:
                sb.AppendLine("auth:bearer {");
                sb.AppendLine($"{Indent}token: {auth.Token}");
                sb.AppendLine("}");
                break;
            case AuthType.Basic:
                sb.AppendLine("auth:basic {");
                sb.AppendLine($"{Indent}username: {auth.Username}");
                sb.AppendLine($"{Indent}password: {auth.Password}");
                sb.AppendLine("}");
                break;
        }
        sb.AppendLine();
    }

    private string MapAuthType(AuthType type) => type switch
    {
        AuthType.Inherit => "inherit",
        AuthType.BearerToken => "bearer",
        AuthType.Basic => "basic",
        _ => "none"
    };

    private string InferBodyType(RequestModel req)
    {
        if (string.IsNullOrWhiteSpace(req.Body)) return "none";

        // Try to infer from headers since RequestModel stores Body as raw string
        var contentType = req.Headers.FirstOrDefault(x => x.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)).Value;

        if (string.IsNullOrEmpty(contentType)) return "text";
        if (contentType.Contains("json")) return "json";
        if (contentType.Contains("xml")) return "xml";
        if (contentType.Contains("form-urlencoded")) return "form-urlencoded";
        if (contentType.Contains("graphql")) return "graphql";

        return "text";
    }
}
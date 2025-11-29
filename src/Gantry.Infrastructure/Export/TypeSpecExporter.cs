using Gantry.Core.Domain.Collections;
using Gantry.Core.Domain.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Gantry.Infrastructure.Export;

public class TypeSpecExporter : ICollectionExporter
{
    public string Name => "TypeSpec";
    public string Extension => "tsp";

    // Buffer to hold generated model definitions so we can append them at the end of the file
    private readonly StringBuilder _modelBuffer = new();
    private readonly HashSet<string> _usedModelNames = new();

    public Task<byte[]> ExportAsync(Collection collection)
    {
        _modelBuffer.Clear();
        _usedModelNames.Clear();

        var sb = new StringBuilder();

        // 1. Headers & Imports
        sb.AppendLine("import \"@typespec/http\";");
        sb.AppendLine("using TypeSpec.Http;");
        sb.AppendLine();

        var safeNamespace = SanitizeIdentifier(collection.Name);
        sb.AppendLine($"namespace {safeNamespace};");
        sb.AppendLine();

        // 2. Process all requests (recursively find them)
        var allRequests = GetAllRequests(collection);

        foreach (var item in allRequests)
        {
            WriteOperation(sb, item);
        }

        // 3. Append generated models at the bottom
        if (_modelBuffer.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("// --- Shared Models ---");
            sb.Append(_modelBuffer);
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private void WriteOperation(StringBuilder sb, RequestItem item)
    {
        var req = item.Request;
        var opName = SanitizeIdentifier(item.Name);
        var method = req.Method.ToLower();

        sb.AppendLine($"// {item.Name}");

        // Handle Route (Strip protocol/host if strictly adhering to relative paths, or keep as absolute string)
        var route = req.Url;
        if (Uri.TryCreate(req.Url, UriKind.Absolute, out var uri))
        {
            route = uri.AbsolutePath;
        }
        sb.AppendLine($"@route(\"{route}\")");

        // Handle Verb Decorator
        sb.AppendLine($"@{method}");

        // Build Signature
        // op Name(@body body: Model): void;
        sb.Append($"op {opName}(");

        bool hasParams = false;

        // If body exists, generate a model for it
        if (CanHaveBody(method) && !string.IsNullOrWhiteSpace(req.Body))
        {
            var modelName = $"{opName}Request";
            if (TryGenerateModel(modelName, req.Body))
            {
                sb.Append($"@body body: {modelName}");
                hasParams = true;
            }
        }

        // TODO: We need to also parse Query parameters and add them as arguments
        // e.g. @query id: string

        sb.AppendLine("): void;");
        sb.AppendLine();
    }

    private bool TryGenerateModel(string desiredName, string jsonBody)
    {
        try
        {
            var node = JsonNode.Parse(jsonBody);
            if (node is JsonObject obj)
            {
                GenerateTypeSpecModel(desiredName, obj);
                return true;
            }
            else if (node is JsonArray arr && arr.Count > 0 && arr[0] is JsonObject arrObj)
            {
                // If the root body is an array, we generate a model for the item
                GenerateTypeSpecModel(desiredName + "Item", arrObj);
                // But we don't return true for the *request* parameter being the model, 
                // the caller would need to handle "body: Model[]". 
                // For simplicity in this generator, we only generate named models for Objects.
                return false;
            }
        }
        catch
        {
            // Invalid JSON, cannot generate model
        }
        return false;
    }

    private void GenerateTypeSpecModel(string modelName, JsonObject jsonObject)
    {
        modelName = GetUniqueModelName(modelName);

        _modelBuffer.AppendLine($"model {modelName} {{");

        foreach (var prop in jsonObject)
        {
            if (prop.Value == null) continue;

            var propName = SanitizeIdentifier(prop.Key);
            var type = InferType(prop.Key, prop.Value, modelName); // Pass parent name to help naming nested models

            _modelBuffer.AppendLine($"  {propName}: {type};");
        }

        _modelBuffer.AppendLine("}");
        _modelBuffer.AppendLine();
    }

    private string InferType(string propName, JsonNode node, string parentModelName)
    {
        switch (node)
        {
            case JsonObject obj:
                var nestedName = $"{parentModelName}{SanitizeIdentifier(propName, true)}";
                GenerateTypeSpecModel(nestedName, obj);
                return nestedName;

            case JsonArray arr:
                if (arr.Count > 0 && arr[0] != null)
                {
                    var itemType = InferType(propName, arr[0]!, parentModelName);
                    return $"{itemType}[]";
                }
                return "string[]"; // Fallback for empty array

            case JsonValue val:
                if (val.TryGetValue<int>(out _) || val.TryGetValue<long>(out _)) return "int32";
                if (val.TryGetValue<double>(out _) || val.TryGetValue<float>(out _)) return "float64";
                if (val.TryGetValue<bool>(out _)) return "boolean";
                // Simple ISO date check could go here
                return "string";

            default:
                return "string";
        }
    }

    // --- Helpers ---

    private string GetUniqueModelName(string baseName)
    {
        if (!_usedModelNames.Contains(baseName))
        {
            _usedModelNames.Add(baseName);
            return baseName;
        }

        int i = 1;
        while (_usedModelNames.Contains($"{baseName}{i}")) i++;

        var newName = $"{baseName}{i}";
        _usedModelNames.Add(newName);
        return newName;
    }

    private string SanitizeIdentifier(string input, bool capitalize = false)
    {
        if (string.IsNullOrEmpty(input)) return "Unknown";

        // Remove invalid chars
        var clean = Regex.Replace(input, "[^a-zA-Z0-9_]", "");

        // Ensure it doesn't start with a number
        if (char.IsDigit(clean[0])) clean = "_" + clean;

        if (capitalize && clean.Length > 0)
        {
            clean = char.ToUpper(clean[0]) + clean.Substring(1);
        }

        return clean;
    }

    private IEnumerable<RequestItem> GetAllRequests(Collection collection)
    {
        foreach (var r in collection.Requests) yield return r;
        foreach (var sub in collection.SubCollections)
            foreach (var r in GetAllRequests(sub)) yield return r;
    }

    private bool CanHaveBody(string method) => method == "post" || method == "put" || method == "patch";
}
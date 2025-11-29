using Gantry.Core.Domain.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Gantry.Infrastructure.Export;

public class JsonSchemaExporter : ICollectionExporter
{
    public string Name => "JSON Schema";
    public string Extension => "json";

    public Task<byte[]> ExportAsync(Collection collection)
    {
        // Root structure for a standard JSON Schema file containing multiple definitions
        var root = new JsonObject
        {
            ["$schema"] = "http://json-schema.org/draft-07/schema#",
            ["title"] = $"{collection.Name} Definitions",
            ["type"] = "object",
            ["definitions"] = new JsonObject()
        };

        var definitions = root["definitions"]!.AsObject();

        // 1. Flatten all requests to find valid JSON bodies
        var allRequests = GetAllRequests(collection);

        foreach (var item in allRequests)
        {
            // Skip empty bodies
            if (string.IsNullOrWhiteSpace(item.Request.Body))
                continue;

            try
            {
                // 2. Parse the body strictly as a Node tree
                var jsonNode = JsonNode.Parse(item.Request.Body);

                if (jsonNode != null)
                {
                    // 3. Infer schema structure recursively
                    var schema = InferSchema(jsonNode);

                    // Sanitize name for use as a key (remove spaces/special chars)
                    var safeName = SanitizeKey(item.Name);

                    // Handle duplicate names by appending increment
                    int count = 1;
                    var finalKey = safeName;
                    while (definitions.ContainsKey(finalKey))
                    {
                        finalKey = $"{safeName}_{count++}";
                    }

                    definitions.Add(finalKey, schema);
                }
            }
            catch (JsonException)
            {
                // Body was not valid JSON; skip or log
            }
        }

        // 4. Serialize to Byte Array
        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonString = root.ToJsonString(options);

        return Task.FromResult(Encoding.UTF8.GetBytes(jsonString));
    }

    /// <summary>
    /// Recursively analyzes a JsonNode to generate a Schema definition.
    /// </summary>
    private JsonObject InferSchema(JsonNode node)
    {
        var schema = new JsonObject();

        switch (node)
        {
            case JsonObject obj:
                schema["type"] = "object";
                var properties = new JsonObject();
                schema["properties"] = properties;

                foreach (var property in obj)
                {
                    // Recursion occurs here
                    if (property.Value != null)
                    {
                        properties.Add(property.Key, InferSchema(property.Value));
                    }
                }
                break;

            case JsonArray arr:
                schema["type"] = "array";
                if (arr.Count > 0 && arr[0] != null)
                {
                    // Infer type from the first item in the array
                    schema["items"] = InferSchema(arr[0]!);
                }
                else
                {
                    // Empty array, generic items
                    schema["items"] = new JsonObject { ["type"] = "string" };
                }
                break;

            case JsonValue val:
                schema["type"] = InferPrimitiveType(val);
                break;

            default:
                schema["type"] = "string";
                break;
        }

        return schema;
    }

    private string InferPrimitiveType(JsonValue value)
    {
        if (value.TryGetValue<int>(out _) || value.TryGetValue<long>(out _)) return "integer";
        if (value.TryGetValue<double>(out _) || value.TryGetValue<float>(out _)) return "number";
        if (value.TryGetValue<bool>(out _)) return "boolean";
        return "string";
    }

    private IEnumerable<RequestItem> GetAllRequests(Collection collection)
    {
        foreach (var req in collection.Requests) yield return req;

        foreach (var sub in collection.SubCollections)
        {
            foreach (var req in GetAllRequests(sub)) yield return req;
        }
    }

    private string SanitizeKey(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, "[^a-zA-Z0-9]", "");
    }
}
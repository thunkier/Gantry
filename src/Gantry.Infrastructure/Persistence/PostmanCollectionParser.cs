using Gantry.Core.Domain.Collections;
using Gantry.Core.Domain.Http;
using Gantry.Core.Domain.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Gantry.Infrastructure.Persistence;

public class PostmanCollectionParser
{
    public Collection Parse(string jsonContent)
    {
        var root = JsonNode.Parse(jsonContent);
        if (root == null) return new Collection();

        var collection = new Collection();

        // Info
        var info = root["info"];
        if (info != null)
        {
            collection.Name = info["name"]?.ToString() ?? "Imported Postman Collection";
            collection.PostmanId = info["_postman_id"]?.ToString() ?? "";
            collection.SchemaUrl = info["schema"]?.ToString() ?? "";

            var descNode = info["description"];
            collection.Description = ParseDescription(descNode);
        }

        // Variables (Collection Variables)
        ProcessVariables(root, collection.Variables);

        // Headers (Collection Level - not standard Postman but good to support if present)
        ProcessHeaders(root, collection.Headers);

        // Auth (Collection Level)
        var auth = root["auth"];
        if (auth != null)
        {
            ProcessAuth(auth, collection.Auth);
        }

        // Items
        var itemArray = root["item"] as JsonArray;
        if (itemArray != null)
        {
            ProcessItems(itemArray, collection);
        }

        return collection;
    }

    private void ProcessItems(JsonArray items, Collection parentCollection)
    {
        foreach (var item in items)
        {
            if (item == null) continue;

            var name = item["name"]?.ToString() ?? "Untitled";
            var descNode = item["description"];
            var description = ParseDescription(descNode);

            // Check if folder or request
            if (item["item"] is JsonArray subItems)
            {
                // Folder
                var folder = new Collection
                {
                    Name = name,
                    Description = description,
                    Parent = parentCollection
                };

                // Folder Auth
                var auth = item["auth"];
                if (auth != null)
                {
                    ProcessAuth(auth, folder.Auth);
                }

                // Folder Variables
                ProcessVariables(item, folder.Variables);

                // Folder Headers
                ProcessHeaders(item, folder.Headers);

                // Folder Events (Scripts)
                ProcessEvents(item, folder.Scripts);

                ProcessItems(subItems, folder);
                parentCollection.SubCollections.Add(folder);
            }
            else if (item["request"] != null)
            {
                // Request
                var requestNode = item["request"];
                var requestItem = new RequestItem
                {
                    Name = name,
                    Description = description,
                    Parent = parentCollection
                };

                // Request Auth
                var auth = requestNode?["auth"];
                if (auth != null)
                {
                    ProcessAuth(auth, requestItem.Auth);
                }

                // Request Description (override item description if present in request)
                var reqDescNode = requestNode?["description"];
                if (reqDescNode != null)
                {
                    requestItem.Description = ParseDescription(reqDescNode);
                }

                // Method
                requestItem.Request.Method = requestNode?["method"]?.ToString() ?? "GET";

                // URL
                ProcessUrl(requestNode, requestItem);

                // Headers
                ProcessHeaders(requestNode, requestItem.Headers, requestItem.Request.Headers);

                // Body
                ProcessBody(requestNode, requestItem);

                // Proxy
                var proxyNode = requestNode?["proxy"];
                if (proxyNode != null)
                {
                    requestItem.Request.Proxy = new ProxyConfig
                    {
                        Match = proxyNode["match"]?.ToString() ?? "http+https://*/*",
                        Host = proxyNode["host"]?.ToString() ?? "",
                        Port = proxyNode["port"]?.GetValue<int>() ?? 8080,
                        Tunnel = proxyNode["tunnel"]?.GetValue<bool>() ?? false,
                        Disabled = proxyNode["disabled"]?.GetValue<bool>() ?? false
                    };
                }

                // Certificate
                var certNode = requestNode?["certificate"];
                if (certNode != null)
                {
                    requestItem.Request.Certificate = new CertificateConfig
                    {
                        Name = certNode["name"]?.ToString() ?? "",
                        KeySrc = certNode["key"]?["src"]?.ToString() ?? "",
                        CertSrc = certNode["cert"]?["src"]?.ToString() ?? "",
                        Passphrase = certNode["passphrase"]?.ToString() ?? ""
                    };

                    var matches = certNode["matches"] as JsonArray;
                    if (matches != null)
                    {
                        requestItem.Request.Certificate.Matches = matches.Select(m => m?.ToString() ?? "").ToList();
                    }
                }

                // Protocol Profile Behavior
                var protocolNode = item["protocolProfileBehavior"];
                if (protocolNode is JsonObject protocolObj)
                {
                    foreach (var kvp in protocolObj)
                    {
                        if (kvp.Value is JsonValue val)
                        {
                            // Simple conversion for now
                            if (val.TryGetValue<bool>(out var b)) requestItem.ProtocolProfileBehavior[kvp.Key] = b;
                            else if (val.TryGetValue<int>(out var i)) requestItem.ProtocolProfileBehavior[kvp.Key] = i;
                            else requestItem.ProtocolProfileBehavior[kvp.Key] = val.ToString();
                        }
                    }
                }

                // Scripts (Events)
                ProcessEvents(item, requestItem.Scripts);

                parentCollection.Requests.Add(requestItem);
            }
        }
    }

    private string ParseDescription(JsonNode? descNode)
    {
        if (descNode == null) return "";

        if (descNode is JsonValue val)
        {
            return val.ToString();
        }
        else if (descNode["content"] != null)
        {
            return descNode["content"]?.ToString() ?? "";
        }
        return "";
    }

    private void ProcessUrl(JsonNode? requestNode, RequestItem requestItem)
    {
        var urlNode = requestNode?["url"];
        if (urlNode is JsonObject urlObj)
        {
            // Always parse query params if present
            var queryNode = urlObj["query"];
            if (queryNode is JsonArray queryArray)
            {
                var queryParams = new List<string>();
                foreach (var q in queryArray)
                {
                    var k = q?["key"]?.ToString();
                    var v = q?["value"]?.ToString();
                    var disabled = q?["disabled"]?.ToString()?.ToLower() == "true";
                    var desc = ParseDescription(q?["description"]);

                    if (!string.IsNullOrEmpty(k))
                    {
                        if (!disabled)
                        {
                            queryParams.Add($"{k}={v}");
                        }

                        requestItem.Params.Add(new ParamItem
                        {
                            Key = k,
                            Value = v ?? "",
                            IsActive = !disabled
                        });
                    }
                }
            }

            if (urlObj["raw"] != null)
            {
                requestItem.Request.Url = urlObj["raw"]?.ToString() ?? "";
            }
            else
            {
                // Construct URL from parts
                var hostNode = urlObj["host"];
                var pathNode = urlObj["path"];

                var host = "";
                if (hostNode is JsonArray hostArray)
                {
                    host = string.Join("", hostArray.Select(x => x?.ToString()));
                }
                else if (hostNode != null)
                {
                    host = hostNode.ToString();
                }

                var path = "";
                if (pathNode is JsonArray pathArray)
                {
                    path = string.Join("/", pathArray.Select(x => x?.ToString()));
                }
                else if (pathNode != null)
                {
                    path = pathNode.ToString();
                }

                // Reconstruct query string from params if needed
                var queryString = "";
                var activeParams = requestItem.Params.Where(p => p.IsActive).Select(p => $"{p.Key}={p.Value}");
                if (activeParams.Any())
                {
                    queryString = "?" + string.Join("&", activeParams);
                }

                // Combine
                var fullUrl = host;
                if (!string.IsNullOrEmpty(path))
                {
                    if (!fullUrl.EndsWith("/") && !path.StartsWith("/"))
                    {
                        fullUrl += "/" + path;
                    }
                    else if (fullUrl.EndsWith("/") && path.StartsWith("/"))
                    {
                        fullUrl += path.Substring(1);
                    }
                    else
                    {
                        fullUrl += path;
                    }
                }
                fullUrl += queryString;

                requestItem.Request.Url = fullUrl;
            }
        }
        else if (urlNode is JsonValue urlVal)
        {
            requestItem.Request.Url = urlVal.ToString();
        }
    }

    private void ProcessBody(JsonNode? requestNode, RequestItem requestItem)
    {
        var bodyNode = requestNode?["body"];
        if (bodyNode != null)
        {
            var mode = bodyNode["mode"]?.ToString();
            if (mode == "raw")
            {
                requestItem.Request.Body = bodyNode["raw"]?.ToString() ?? "";

                // Try to infer Content-Type if not present
                var options = bodyNode["options"];
                var language = options?["raw"]?["language"]?.ToString();
                if (language == "json")
                {
                    if (!requestItem.Headers.Any(h => h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)))
                    {
                        var ct = "application/json";
                        requestItem.Headers.Add(new HeaderItem { Key = "Content-Type", Value = ct, IsActive = true });
                        requestItem.Request.Headers["Content-Type"] = ct;
                    }
                }
            }
            else if (mode == "urlencoded")
            {
                // TODO: Handle urlencoded body specifically if needed, for now we might want to represent it as a string or specific model
                // For now, let's just try to serialize it to string if possible or leave empty
                // Ideally we should have a specific property for this in RequestModel
            }
            else if (mode == "formdata")
            {
                // TODO: Handle formdata
            }
            else if (mode == "file")
            {
                // TODO: Handle file
            }
            else if (mode == "graphql")
            {
                var graphql = bodyNode["graphql"];
                if (graphql != null)
                {
                    var query = graphql["query"]?.ToString() ?? "";
                    var variables = graphql["variables"]?.ToString() ?? "";
                    requestItem.Request.Body = JsonSerializer.Serialize(new { query, variables });

                    if (!requestItem.Headers.Any(h => h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)))
                    {
                        var ct = "application/json";
                        requestItem.Headers.Add(new HeaderItem { Key = "Content-Type", Value = ct, IsActive = true });
                        requestItem.Request.Headers["Content-Type"] = ct;
                    }
                }
            }
        }
    }

    private void ProcessEvents(JsonNode? item, ScriptSettings scripts)
    {
        var eventArray = item?["event"] as JsonArray;
        if (eventArray != null)
        {
            foreach (var evt in eventArray)
            {
                var listen = evt?["listen"]?.ToString();
                var scriptNode = evt?["script"];
                if (scriptNode != null)
                {
                    var execNode = scriptNode["exec"];
                    var scriptContent = "";

                    if (execNode is JsonArray execArray)
                    {
                        scriptContent = string.Join("\n", execArray.Select(x => x?.ToString()));
                    }
                    else if (execNode != null)
                    {
                        scriptContent = execNode.ToString();
                    }

                    if (!string.IsNullOrWhiteSpace(scriptContent))
                    {
                        if (listen == "prerequest")
                        {
                            scripts.PreRequestScript = scriptContent;
                        }
                        else if (listen == "test")
                        {
                            scripts.PostResponseScript = scriptContent;
                        }
                    }
                }
            }
        }
    }

    private void ProcessVariables(JsonNode node, System.Collections.ObjectModel.ObservableCollection<Variable> variables)
    {
        if (node == null) return;

        var variableArray = node["variable"] as JsonArray;
        if (variableArray != null)
        {
            foreach (var v in variableArray)
            {
                if (v != null)
                {
                    variables.Add(new Variable
                    {
                        Key = v["key"]?.ToString() ?? "",
                        Value = v["value"]?.ToString() ?? "",
                        Type = v["type"]?.ToString() ?? "string",
                        Description = ParseDescription(v["description"]),
                        Id = v["id"]?.ToString() ?? "",
                        Enabled = !(v["disabled"]?.GetValue<bool>() ?? false)
                    });
                }
            }
        }
    }

    private void ProcessHeaders(JsonNode? node, System.Collections.ObjectModel.ObservableCollection<HeaderItem> headerItems, Dictionary<string, string>? requestHeadersDict = null)
    {
        if (node == null) return;

        var headerArray = node["header"] as JsonArray;
        if (headerArray != null)
        {
            foreach (var h in headerArray)
            {
                var key = h?["key"]?.ToString();
                var value = h?["value"]?.ToString();
                var disabled = h?["disabled"]?.GetValue<bool>() ?? false;
                var description = ParseDescription(h?["description"]);

                if (!string.IsNullOrEmpty(key))
                {
                    if (requestHeadersDict != null && !disabled)
                    {
                        requestHeadersDict[key] = value ?? "";
                    }

                    headerItems.Add(new HeaderItem
                    {
                        Key = key,
                        Value = value ?? "",
                        IsActive = !disabled,
                        Description = description
                    });
                }
            }
        }
    }

    private void ProcessAuth(JsonNode authNode, AuthSettings authSettings)
    {
        var type = authNode["type"]?.ToString();

        // Helper to get attributes
        Dictionary<string, string> GetAttributes(string authType)
        {
            var attrs = new Dictionary<string, string>();
            var typeNode = authNode[authType] as JsonArray;
            if (typeNode != null)
            {
                foreach (var item in typeNode)
                {
                    var key = item?["key"]?.ToString();
                    var val = item?["value"]?.ToString();
                    if (!string.IsNullOrEmpty(key))
                    {
                        attrs[key] = val ?? "";
                    }
                }
            }
            return attrs;
        }

        if (type == "bearer")
        {
            authSettings.Type = AuthType.BearerToken;
            var attrs = GetAttributes("bearer");
            if (attrs.TryGetValue("token", out var token)) authSettings.Token = token;
            authSettings.Attributes = attrs;
        }
        else if (type == "basic")
        {
            authSettings.Type = AuthType.Basic;
            var attrs = GetAttributes("basic");
            if (attrs.TryGetValue("username", out var u)) authSettings.Username = u;
            if (attrs.TryGetValue("password", out var p)) authSettings.Password = p;
            authSettings.Attributes = attrs;
        }
        else if (type == "noauth")
        {
            authSettings.Type = AuthType.None;
        }
        else if (type == "apikey")
        {
            authSettings.Type = AuthType.ApiKey;
            authSettings.Attributes = GetAttributes("apikey");
        }
        else if (type == "awsv4")
        {
            authSettings.Type = AuthType.AwsV4;
            authSettings.Attributes = GetAttributes("awsv4");
        }
        else if (type == "digest")
        {
            authSettings.Type = AuthType.Digest;
            authSettings.Attributes = GetAttributes("digest");
        }
        else if (type == "edgegrid")
        {
            authSettings.Type = AuthType.EdgeGrid;
            authSettings.Attributes = GetAttributes("edgegrid");
        }
        else if (type == "hawk")
        {
            authSettings.Type = AuthType.Hawk;
            authSettings.Attributes = GetAttributes("hawk");
        }
        else if (type == "ntlm")
        {
            authSettings.Type = AuthType.Ntlm;
            authSettings.Attributes = GetAttributes("ntlm");
        }
        else if (type == "oauth1")
        {
            authSettings.Type = AuthType.OAuth1;
            authSettings.Attributes = GetAttributes("oauth1");
        }
        else if (type == "oauth2")
        {
            authSettings.Type = AuthType.OAuth2;
            authSettings.Attributes = GetAttributes("oauth2");
        }
        // Default is Inherit if not specified or unrecognized
    }
}

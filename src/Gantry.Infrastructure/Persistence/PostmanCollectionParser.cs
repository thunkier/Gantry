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

            // Check if folder or request
            if (item["item"] is JsonArray subItems)
            {
                // Folder
                var folder = new Collection
                {
                    Name = name,
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
                    Parent = parentCollection
                };

                // Request Auth
                var auth = requestNode?["auth"];
                if (auth != null)
                {
                    ProcessAuth(auth, requestItem.Auth);
                }

                // Description
                var descNode = requestNode?["description"];
                if (descNode != null)
                {
                    if (descNode is JsonValue val)
                    {
                        requestItem.Description = val.ToString();
                    }
                    else if (descNode["content"] != null)
                    {
                        requestItem.Description = descNode["content"]?.ToString() ?? "";
                    }
                }

                // Method
                requestItem.Request.Method = requestNode?["method"]?.ToString() ?? "GET";

                // URL
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

                // Headers
                ProcessHeaders(requestNode, requestItem.Headers, requestItem.Request.Headers);

                // Body
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
                    // TODO: Support other body modes like formdata, urlencoded
                }

                // Scripts (Events)
                var eventArray = item["event"] as JsonArray;
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
                                    requestItem.Scripts.PreRequestScript = scriptContent;
                                }
                                else if (listen == "test")
                                {
                                    requestItem.Scripts.PostResponseScript = scriptContent;
                                }
                            }
                        }
                    }
                }

                parentCollection.Requests.Add(requestItem);
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
                        Enabled = true
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
                if (!string.IsNullOrEmpty(key))
                {
                    if (requestHeadersDict != null)
                    {
                        requestHeadersDict[key] = value ?? "";
                    }

                    headerItems.Add(new HeaderItem
                    {
                        Key = key,
                        Value = value ?? "",
                        IsActive = true
                    });
                }
            }
        }
    }

    private void ProcessAuth(JsonNode authNode, AuthSettings authSettings)
    {
        var type = authNode["type"]?.ToString();
        if (type == "bearer")
        {
            authSettings.Type = AuthType.BearerToken;
            var bearer = authNode["bearer"] as JsonArray;
            if (bearer != null)
            {
                foreach (var b in bearer)
                {
                    if (b?["key"]?.ToString() == "token")
                    {
                        authSettings.Token = b["value"]?.ToString() ?? "";
                        break;
                    }
                }
            }
        }
        else if (type == "basic")
        {
            authSettings.Type = AuthType.Basic;
            var basic = authNode["basic"] as JsonArray;
            if (basic != null)
            {
                foreach (var b in basic)
                {
                    if (b?["key"]?.ToString() == "username") authSettings.Username = b["value"]?.ToString() ?? "";
                    if (b?["key"]?.ToString() == "password") authSettings.Password = b["value"]?.ToString() ?? "";
                }
            }
        }
        else if (type == "noauth")
        {
            authSettings.Type = AuthType.None;
        }
        // Default is Inherit if not specified or unrecognized
    }
}

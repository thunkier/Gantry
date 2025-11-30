using Gantry.Core.Domain.Collections;
using Gantry.Core.Domain.Settings;
using Gantry.Infrastructure.Persistence;
using System.Linq;
using Xunit;

namespace Gantry.Tests;

public class PostmanCollectionV21Tests
{
    [Fact]
    public void Parse_ShouldPopulateCollectionInfo()
    {
        var parser = new PostmanCollectionParser();
        var json = @"{
            ""info"": {
                ""name"": ""Test Collection"",
                ""description"": ""A test collection"",
                ""_postman_id"": ""1234-5678"",
                ""schema"": ""https://schema.getpostman.com/json/collection/v2.1.0/""
            },
            ""item"": []
        }";

        var collection = parser.Parse(json);

        Assert.Equal("Test Collection", collection.Name);
        Assert.Equal("A test collection", collection.Description);
        Assert.Equal("1234-5678", collection.PostmanId);
        Assert.Equal("https://schema.getpostman.com/json/collection/v2.1.0/", collection.SchemaUrl);
    }

    [Fact]
    public void Parse_ShouldPopulateAuth()
    {
        var parser = new PostmanCollectionParser();
        var json = @"{
            ""info"": { ""name"": ""Auth Collection"" },
            ""auth"": {
                ""type"": ""apikey"",
                ""apikey"": [
                    { ""key"": ""key"", ""value"": ""my-api-key"" },
                    { ""key"": ""value"", ""value"": ""secret"" }
                ]
            },
            ""item"": []
        }";

        var collection = parser.Parse(json);

        Assert.Equal(AuthType.ApiKey, collection.Auth.Type);
        Assert.True(collection.Auth.Attributes.ContainsKey("key"));
        Assert.Equal("my-api-key", collection.Auth.Attributes["key"]);
    }

    [Fact]
    public void Parse_ShouldPopulateRequestDetails()
    {
        var parser = new PostmanCollectionParser();
        var json = @"{
            ""info"": { ""name"": ""Request Collection"" },
            ""item"": [
                {
                    ""name"": ""Complex Request"",
                    ""request"": {
                        ""method"": ""POST"",
                        ""url"": ""https://example.com"",
                        ""proxy"": {
                            ""host"": ""127.0.0.1"",
                            ""port"": 8080,
                            ""tunnel"": true
                        },
                        ""certificate"": {
                            ""name"": ""Client Cert"",
                            ""matches"": [""https://example.com/*""],
                            ""key"": { ""src"": ""/path/to/key"" },
                            ""cert"": { ""src"": ""/path/to/cert"" }
                        }
                    }
                }
            ]
        }";

        var collection = parser.Parse(json);
        var request = collection.Requests.First();

        Assert.NotNull(request.Request.Proxy);
        Assert.Equal("127.0.0.1", request.Request.Proxy.Host);
        Assert.Equal(8080, request.Request.Proxy.Port);
        Assert.True(request.Request.Proxy.Tunnel);

        Assert.NotNull(request.Request.Certificate);
        Assert.Equal("Client Cert", request.Request.Certificate.Name);
        Assert.Contains("https://example.com/*", request.Request.Certificate.Matches);
        Assert.Equal("/path/to/key", request.Request.Certificate.KeySrc);
    }

    [Fact]
    public void Parse_ShouldHandleDisabledItems()
    {
        var parser = new PostmanCollectionParser();
        var json = @"{
            ""info"": { ""name"": ""Disabled Items"" },
            ""variable"": [
                { ""key"": ""var1"", ""value"": ""val1"", ""disabled"": true }
            ],
            ""item"": [
                {
                    ""name"": ""Req"",
                    ""request"": {
                        ""url"": ""https://example.com"",
                        ""header"": [
                            { ""key"": ""h1"", ""value"": ""v1"", ""disabled"": true }
                        ]
                    }
                }
            ]
        }";

        var collection = parser.Parse(json);

        Assert.Single(collection.Variables);
        Assert.False(collection.Variables[0].Enabled);

        var request = collection.Requests.First();
        Assert.Single(request.Headers);
        Assert.True(request.Headers[0].Disabled);
        Assert.False(request.Headers[0].IsActive);
    }
}

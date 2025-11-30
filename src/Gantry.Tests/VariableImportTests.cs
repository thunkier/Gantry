using Gantry.Infrastructure.Persistence;
using Xunit;
using System.Linq;

namespace Gantry.Tests;

public class VariableImportTests
{
    [Fact]
    public void Parse_ShouldImportVariablesWithDotsInKey()
    {
        var json = @"
{
    ""info"": {
        ""name"": ""Test Collection"",
        ""schema"": ""https://schema.getpostman.com/json/collection/v2.1.0/collection.json""
    },
    ""variable"": [
        {
            ""key"": ""Metrc.api.server"",
            ""value"": ""https://example.com"",
            ""type"": ""string""
        },
        {
            ""key"": ""Metrc.userKey"",
            ""value"": ""11AABBCC"",
            ""type"": ""string""
        }
    ],
    ""item"": []
}";

        var parser = new PostmanCollectionParser();
        var collection = parser.Parse(json);

        Assert.Equal(2, collection.Variables.Count);
        Assert.Contains(collection.Variables, v => v.Key == "Metrc.api.server");
        Assert.Contains(collection.Variables, v => v.Key == "Metrc.userKey");
    }
}

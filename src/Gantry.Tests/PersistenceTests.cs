using Gantry.Core.Domain.Collections;
using Gantry.Core.Domain.Http;
using Gantry.Infrastructure.Persistence;
using System.IO;
using Xunit;

namespace Gantry.Tests;

public class PersistenceTests
{
    [Fact]
    public void SaveAndLoadBundle_ShouldPersistCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "GantryTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var repo = new FileSystemCollectionRepository();

        var request = new RequestItem
        {
            Name = "Test Request",
            Path = Path.Combine(tempDir, "Test Request.req"),
            Request = new RequestModel
            {
                Method = "POST",
                Url = "https://api.example.com/data",
                Body = "{\"key\": \"value\"}"
            }
        };
        request.Headers.Add(new HeaderItem { Key = "Content-Type", Value = "application/json", IsActive = true });
        request.Scripts.PreRequestScript = "console.log('pre');";
        request.Scripts.PostResponseScript = "console.log('post');";

        // Act
        repo.SaveRequest(request);

        // Assert Files
        Assert.True(Directory.Exists(request.Path));
        Assert.True(File.Exists(Path.Combine(request.Path, "meta.toml")));
        Assert.True(File.Exists(Path.Combine(request.Path, "body.json")));
        Assert.True(File.Exists(Path.Combine(request.Path, "pre-script.js")));
        Assert.True(File.Exists(Path.Combine(request.Path, "test.js")));

        // Act - Load
        var loadedCollection = repo.LoadCollection(tempDir);
        var loadedRequest = loadedCollection.Requests.FirstOrDefault();

        // Assert Loaded
        Assert.NotNull(loadedRequest);
        Assert.Equal("Test Request", loadedRequest.Name);
        Assert.Equal("POST", loadedRequest.Request.Method);
        Assert.Equal("https://api.example.com/data", loadedRequest.Request.Url);
        Assert.Equal("{\"key\": \"value\"}", loadedRequest.Request.Body);
        Assert.Equal("console.log('pre');", loadedRequest.Scripts.PreRequestScript);
        Assert.Equal("console.log('post');", loadedRequest.Scripts.PostResponseScript);

        var header = loadedRequest.Headers.FirstOrDefault(h => h.Key == "Content-Type");
        Assert.NotNull(header);
        Assert.Equal("application/json", header.Value);

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void SaveRequest_ShouldEnforceBundleFormat()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "GantryTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var repo = new FileSystemCollectionRepository();

        // Create a request with a path that looks like a file (no extension or .json)
        var request = new RequestItem
        {
            Name = "Legacy Request",
            Path = Path.Combine(tempDir, "Legacy Request"), // No extension
            Request = new RequestModel
            {
                Method = "GET",
                Url = "https://example.com"
            }
        };

        // Act
        repo.SaveRequest(request);

        // Assert
        // The repository should have updated the path to end with .req
        Assert.EndsWith(".req", request.Path);
        Assert.True(Directory.Exists(request.Path));
        Assert.True(File.Exists(Path.Combine(request.Path, "meta.toml")));

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void ImportPostmanCollection_ShouldSaveAsBundle()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "GantryTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var repo = new FileSystemCollectionRepository();
        var parser = new PostmanCollectionParser();

        var json = @"{
            ""info"": { ""name"": ""Test Collection"" },
            ""item"": [
                {
                    ""name"": ""Request 1"",
                    ""request"": {
                        ""method"": ""GET"",
                        ""header"": [
                            { ""key"": ""Content-Type"", ""value"": ""application/json"" }
                        ],
                        ""url"": { ""raw"": ""https://example.com"" }
                    }
                },
                {
                    ""name"": ""Folder 1"",
                    ""item"": [
                        {
                            ""name"": ""Request 2"",
                            ""request"": {
                                ""method"": ""POST"",
                                ""url"": ""https://example.com/2""
                            }
                        }
                    ]
                }
            ]
        }";

        // Act
        var collection = parser.Parse(json);

        // Simulate SidebarViewModel logic
        var safeName = string.Join("_", collection.Name.Split(Path.GetInvalidFileNameChars()));
        collection.Path = Path.Combine(tempDir, safeName);

        repo.SaveCollection(collection);

        // Assert
        Assert.True(Directory.Exists(collection.Path));
        Assert.True(Directory.Exists(Path.Combine(collection.Path, "Request 1.req")));
        Assert.True(Directory.Exists(Path.Combine(collection.Path, "Folder 1")));
        Assert.True(Directory.Exists(Path.Combine(collection.Path, "Folder 1", "Request 2.req")));

        // Verify Headers
        var req1Path = Path.Combine(collection.Path, "Request 1.req");
        var loadedBundle = new RequestBundleRepository().LoadBundle(req1Path, collection);
        Assert.Contains(loadedBundle.Headers, h => h.Key == "Content-Type" && h.Value == "application/json");

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void ImportPostmanCollection_WithSpecialChars_ShouldSaveAndLoadCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "GantryTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var repo = new FileSystemCollectionRepository();
        var parser = new PostmanCollectionParser();

        var json = @"{
            ""info"": { ""name"": ""Test \""Cool\"" Collection"" },
            ""item"": [
                {
                    ""name"": ""Request \""1\"" with quotes"",
                    ""request"": {
                        ""method"": ""GET"",
                        ""url"": ""https://example.com""
                    }
                }
            ]
        }";

        // Act
        var collection = parser.Parse(json);

        var safeName = string.Join("_", collection.Name.Split(Path.GetInvalidFileNameChars()));
        collection.Path = Path.Combine(tempDir, safeName);

        repo.SaveCollection(collection);
    }


    [Fact]
    public void SaveBundle_ShouldHandleCleanBundleLogic()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "GantryTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var collection = new Collection { Name = "CleanBundleTest", Path = Path.Combine(tempDir, "CleanBundleTest") };
        var request = new RequestItem
        {
            Name = "CleanRequest",
            Path = Path.Combine(collection.Path, "CleanRequest.req"),
            Description = "Initial Description",
            Parent = collection
        };
        request.Scripts.PostResponseScript = "console.log('test');";

        var repo = new RequestBundleRepository();
        repo.SaveBundle(request);

        // Assert Files Exist
        Assert.True(File.Exists(Path.Combine(request.Path, "readme.md")));
        Assert.True(File.Exists(Path.Combine(request.Path, "test.js")));

        // Act - Clear content
        request.Description = "";
        request.Scripts.PostResponseScript = "";
        repo.SaveBundle(request);

        // Assert Files Deleted
        Assert.False(File.Exists(Path.Combine(request.Path, "readme.md")));
        Assert.False(File.Exists(Path.Combine(request.Path, "test.js")));

        // Cleanup
        Directory.Delete(tempDir, true);
    }
}

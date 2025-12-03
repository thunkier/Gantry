using Gantry.Infrastructure.Persistence;
using Gantry.Core.Domain.NodeEditor;
using Xunit;
using System.IO;
using System.Linq;

namespace Gantry.Tests
{
    public class NodeTaskLoadingTests
    {
        [Fact]
        public void LoadCollection_ShouldLoadNodeTask_WhenFileIsValid()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "GantryTests", "NodeTaskLoading");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            // Exact JSON from user
            var json = @"{
  ""Name"": ""Task 1"",
  ""Path"": ""C:\\Users\\Admin\\Downloads\\test\\Task 1.json"",
  ""Parent"": {
    ""Parent"": null,
    ""Name"": ""test"",
    ""Path"": ""C:\\Users\\Admin\\Downloads\\test\\"",
    ""Auth"": {
      ""Type"": 0,
      ""Token"": """",
      ""Username"": """",
      ""Password"": """",
      ""Attributes"": {}
    },
    ""Scripts"": {
      ""PreRequestScript"": """",
      ""PostResponseScript"": """"
    },
    ""Headers"": [],
    ""Variables"": []
  },
  ""Auth"": {
    ""Type"": 0,
    ""Token"": """",
    ""Username"": """",
    ""Password"": """",
    ""Attributes"": {}
  },
  ""Scripts"": {
    ""PreRequestScript"": """",
    ""PostResponseScript"": """"
  },
  ""Headers"": [],
  ""Variables"": [],
  ""Nodes"": [
    {
      ""Id"": ""0de4f10d-51f8-45df-a9d4-456cc468f0b3"",
      ""Type"": ""RequestNode"",
      ""X"": 100,
      ""Y"": 100,
      ""RequestItem"": {
        ""Path"": ""C:\\Users\\Admin\\Downloads\\test\\Gantry Test Collection\\Echo Headers.req"",
        ""Name"": ""Echo Headers"",
        ""Description"": """",
        ""Request"": {
          ""Url"": ""https://httpbin.org/headers"",
          ""Method"": ""GET"",
          ""Headers"": {},
          ""Body"": """",
          ""TimeoutMs"": 30000,
          ""Auth"": null,
          ""HttpVersion"": ""HTTP/1.1"",
          ""VersionPolicy"": 0,
          ""EnableSslCertificateVerification"": true,
          ""AutomaticallyFollowRedirects"": true,
          ""FollowOriginalHttpMethod"": false,
          ""FollowAuthorizationHeader"": false,
          ""RemoveRefererHeaderOnRedirect"": false,
          ""EnableStrictHttpParser"": false,
          ""EncodeUrlAutomatically"": true,
          ""DisableCookieJar"": false,
          ""UseServerCipherSuiteDuringHandshake"": false,
          ""MaximumNumberOfRedirects"": 10,
          ""DisabledTlsProtocols"": [],
          ""CipherSuiteSelection"": ""Default"",
          ""Proxy"": null,
          ""Certificate"": null
        },
        ""ProtocolProfileBehavior"": {},
        ""Auth"": {
          ""Type"": 0,
          ""Token"": """",
          ""Username"": """",
          ""Password"": """",
          ""Attributes"": {}
        },
        ""Scripts"": {
          ""PreRequestScript"": """",
          ""PostResponseScript"": """"
        },
        ""Variables"": [],
        ""Params"": [],
        ""Headers"": [
          {
            ""Key"": ""X-Custom-Header"",
            ""Value"": ""{{custom_header_value}}"",
            ""Description"": """",
            ""IsActive"": true,
            ""Disabled"": false
          },
          {
            ""Key"": ""Host"",
            ""Value"": ""\u003Ccalculated when request is sent\u003E"",
            ""Description"": """",
            ""IsActive"": true,
            ""Disabled"": false
          },
          {
            ""Key"": ""User-Agent"",
            ""Value"": ""PostmanRuntime/7.26.8"",
            ""Description"": """",
            ""IsActive"": true,
            ""Disabled"": false
          },
          {
            ""Key"": ""Accept"",
            ""Value"": ""*/*"",
            ""Description"": """",
            ""IsActive"": true,
            ""Disabled"": false
          },
          {
            ""Key"": ""Accept-Encoding"",
            ""Value"": ""gzip, deflate, br"",
            ""Description"": """",
            ""IsActive"": true,
            ""Disabled"": false
          },
          {
            ""Key"": ""Connection"",
            ""Value"": ""keep-alive"",
            ""Description"": """",
            ""IsActive"": true,
            ""Disabled"": false
          },
          {
            ""Key"": ""Gantry-Token"",
            ""Value"": ""\u003Ccalculated when request is sent\u003E"",
            ""Description"": """",
            ""IsActive"": true,
            ""Disabled"": false
          }
        ],
        ""History"": []
      }
    }
  ],
  ""Connections"": []
}";
            var filePath = Path.Combine(tempDir, "Task 1.json");
            File.WriteAllText(filePath, json);

            var repo = new FileSystemCollectionRepository();

            // Act
            var collection = repo.LoadCollection(tempDir);

            // Assert
            Assert.NotNull(collection);
            Assert.Single(collection.NodeGraphs);
            var graph = collection.NodeGraphs.First();
            Assert.Equal("Task 1", graph.Name);
            Assert.Single(graph.Nodes);

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}

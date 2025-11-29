using Xunit;
using Moq;
using Gantry.UI.Features.Requests.ViewModels;
using Gantry.Core.Interfaces;
using Gantry.Core.Domain.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Gantry.Tests
{
    public class RequestViewModelTests
    {
        [Fact]
        public async Task SendRequestCommand_ShouldUpdateResponse_WhenRequestIsSuccessful()
        {
            // Arrange
            var mockHttpService = new Mock<IHttpService>();
            var expectedResponse = new ResponseModel
            {
                StatusCode = 200,
                Body = "{\"foo\":\"bar\"}",
                Headers = new Dictionary<string, string>()
            };

            mockHttpService.Setup(s => s.SendRequestAsync(It.IsAny<RequestModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Setup WorkspaceService
            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GantryTests_ViewModel", System.Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);

            var workspaceService = new Gantry.Infrastructure.Services.WorkspaceService();
            workspaceService.OpenWorkspace(tempDir);

            var mockVariableService = new Mock<IVariableService>();
            mockVariableService.Setup(v => v.ResolveVariables(It.IsAny<string>(), It.IsAny<Gantry.Core.Domain.Settings.ISettingsContainer>()))
                .Returns((string s, Gantry.Core.Domain.Settings.ISettingsContainer c) => s);

            var viewModel = new RequestViewModel(mockHttpService.Object, workspaceService, mockVariableService.Object);
            viewModel.Url = "https://example.com";
            viewModel.SelectedMethod = "GET";
            // Ensure model has a path so Save() works
            viewModel.Model.Path = System.IO.Path.Combine(tempDir, "TestRequest.req");
            viewModel.Model.Name = "TestRequest";

            // Act
            await viewModel.SendRequestCommand.ExecuteAsync(null);

            // Assert
            Assert.NotNull(viewModel.Response);
            Assert.Equal(200, viewModel.Response.StatusCode);
            Assert.Equal("{\"foo\":\"bar\"}", viewModel.Response.Body);
            mockHttpService.Verify(s => s.SendRequestAsync(It.Is<RequestModel>(r => r.Url == "https://example.com" && r.Method == "GET"), It.IsAny<CancellationToken>()), Times.Once);

            // Cleanup
            try { System.IO.Directory.Delete(tempDir, true); } catch { }
        }
    }
}

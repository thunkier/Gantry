using Xunit;
using Gantry.Infrastructure.Network;
using Gantry.Core.Domain.Http;
using System.Net.Http;
using System.Threading.Tasks;

namespace Gantry.Tests
{
    public class HttpServiceTests
    {
        [Fact]
        public async Task SendRequestAsync_ShouldReturnResponse_WhenRequestIsSuccessful()
        {
            // Arrange
            var httpService = new HttpService();
            var request = new RequestModel
            {
                Url = "https://httpbin.org/get",
                Method = "GET"
            };

            // Act
            var response = await httpService.SendRequestAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(200, response.StatusCode);
            Assert.Contains("httpbin.org", response.Body);
        }
    }
}

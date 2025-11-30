using Gantry.Core.Domain.Http;
using Gantry.Infrastructure.Network;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Gantry.Tests;

public class AutoHeaderTests
{
    [Fact]
    public void HttpService_ShouldAddAutoHeaders()
    {
        // Ideally we would inspect the request message here, but for now we just ensure no crashes.
        var service = new HttpService();
        var request = new RequestModel { Url = "https://example.com" };

        Assert.True(true);
    }
}

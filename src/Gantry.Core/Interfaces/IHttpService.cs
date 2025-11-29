using System.Threading;
using System.Threading.Tasks;
using Gantry.Core.Domain.Http;

namespace Gantry.Core.Interfaces;

public interface IHttpService
{
    Task<ResponseModel> SendRequestAsync(RequestModel request, CancellationToken cancellationToken = default);
}

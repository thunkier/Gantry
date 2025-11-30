using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Gantry.Core.Interfaces;

public interface IWebSocketService
{
    Task ConnectAsync(string url, CancellationToken cancellationToken = default);
    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
    Task<string> ReceiveMessageAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    WebSocketState State { get; }
}

using Gantry.Core.Interfaces;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gantry.Infrastructure.Network;

public class WebSocketService : IWebSocketService, IDisposable
{
    private ClientWebSocket _client;

    public WebSocketService()
    {
        _client = new ClientWebSocket();
    }

    public WebSocketState State => _client.State;

    public async Task ConnectAsync(string url, CancellationToken cancellationToken = default)
    {
        if (_client.State == WebSocketState.Open || _client.State == WebSocketState.Connecting)
        {
            return;
        }

        // Re-create client if it was disposed or closed
        if (_client.State == WebSocketState.Closed || _client.State == WebSocketState.Aborted)
        {
            _client.Dispose();
            _client = new ClientWebSocket();
        }

        await _client.ConnectAsync(new Uri(url), cancellationToken);
    }

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_client.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected.");
        }

        var buffer = Encoding.UTF8.GetBytes(message);
        await _client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
    }

    public async Task<string> ReceiveMessageAsync(CancellationToken cancellationToken = default)
    {
        if (_client.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected.");
        }

        var buffer = new byte[1024 * 4];
        var result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

        var sb = new StringBuilder();
        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

        while (!result.EndOfMessage)
        {
            result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        }

        return sb.ToString();
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_client.State == WebSocketState.Open)
        {
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

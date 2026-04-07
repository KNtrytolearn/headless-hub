using System.Net.WebSockets;
using System.Text;

namespace HeadlessHub.Services;

/// <summary>
/// WebSocket service for real-time log streaming
/// </summary>
public class WebSocketLoggerService : BackgroundService
{
    private readonly List<WebSocket> _clients = new();
    private readonly object _lock = new();

    public void AddClient(WebSocket socket)
    {
        lock (_lock)
        {
            _clients.Add(socket);
        }
    }

    public void RemoveClient(WebSocket socket)
    {
        lock (_lock)
        {
            _clients.Remove(socket);
        }
    }

    public async Task BroadcastAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);

        List<WebSocket> clientsCopy;
        lock (_lock)
        {
            clientsCopy = _clients.ToList();
        }

        var deadClients = new List<WebSocket>();

        foreach (var client in clientsCopy)
        {
            try
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else if (client.State != WebSocketState.Connecting)
                {
                    deadClients.Add(client);
                }
            }
            catch
            {
                deadClients.Add(client);
            }
        }

        // Clean up disconnected clients
        foreach (var dead in deadClients)
        {
            RemoveClient(dead);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}

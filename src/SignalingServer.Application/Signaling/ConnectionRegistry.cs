using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace SignalingServer.Application.Signaling;

public sealed class ConnectionRegistry : IConnectionRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ManagedConnection>> _rooms = new();

    public void Register(string roomCode, string deviceId, WebSocket socket)
    {
        var room = _rooms.GetOrAdd(roomCode, static _ => new ConcurrentDictionary<string, ManagedConnection>());
        room[deviceId] = new ManagedConnection(socket);
    }

    public void Unregister(string roomCode, string deviceId)
    {
        if (!_rooms.TryGetValue(roomCode, out var room))
        {
            return;
        }

        room.TryRemove(deviceId, out _);
        if (room.IsEmpty)
        {
            _rooms.TryRemove(roomCode, out _);
        }
    }

    public async Task<bool> SendAsync(string roomCode, string deviceId, string json, CancellationToken ct)
    {
        if (!_rooms.TryGetValue(roomCode, out var room) || !room.TryGetValue(deviceId, out var connection))
        {
            return false;
        }

        return await connection.SendAsync(json, ct);
    }

    /// <summary>
    /// WebSocket.SendAsync must not be called concurrently on the same socket
    /// (a relayed message and a server-originated peer-joined/peer-left/error
    /// notification could race). This serializes sends per connection.
    /// </summary>
    private sealed class ManagedConnection(WebSocket socket)
    {
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public async Task<bool> SendAsync(string json, CancellationToken ct)
        {
            if (socket.State != WebSocketState.Open)
            {
                return false;
            }

            var bytes = Encoding.UTF8.GetBytes(json);
            await _sendLock.WaitAsync(ct);
            try
            {
                if (socket.State != WebSocketState.Open)
                {
                    return false;
                }

                await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
                return true;
            }
            finally
            {
                _sendLock.Release();
            }
        }
    }
}

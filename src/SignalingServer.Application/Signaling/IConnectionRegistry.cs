using System.Net.WebSockets;

namespace SignalingServer.Application.Signaling;

/// <summary>
/// Tracks the live WebSocket connection for each (roomCode, deviceId) pair so
/// the relay layer can find where to forward a signaling message.
/// </summary>
public interface IConnectionRegistry
{
    void Register(string roomCode, string deviceId, WebSocket socket);

    void Unregister(string roomCode, string deviceId);

    Task<bool> SendAsync(string roomCode, string deviceId, string json, CancellationToken ct);
}

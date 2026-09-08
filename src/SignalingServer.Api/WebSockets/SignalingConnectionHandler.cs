using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SignalingServer.Api.Contracts;
using SignalingServer.Application.Rooms;
using SignalingServer.Application.Signaling;

namespace SignalingServer.Api.WebSockets;

/// <summary>
/// Owns the receive loop for a single WS connection. Offer/answer/ice-candidate frames
/// are relayed to the peer byte-for-byte (the server never parses SDP); join/leave
/// transitions are turned into peer-joined/peer-left notifications.
/// </summary>
public sealed class SignalingConnectionHandler(
    IRoomService roomService,
    IConnectionRegistry connectionRegistry,
    ISignalingRelayService relayService,
    ILogger<SignalingConnectionHandler> logger)
{
    private const int MaxMessageBytes = 64 * 1024;

    public async Task HandleAsync(WebSocket socket, string roomCode, string deviceId, CancellationToken ct)
    {
        var joinResult = roomService.TryJoin(roomCode, deviceId);
        if (joinResult.Outcome is JoinOutcome.RoomNotFound)
        {
            await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "room-not-found", ct);
            return;
        }

        if (joinResult.Outcome is JoinOutcome.RoomFull)
        {
            await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "room-full", ct);
            return;
        }

        connectionRegistry.Register(roomCode, deviceId, socket);
        logger.LogInformation("Device {DeviceId} {Outcome} room {RoomCode}", deviceId, joinResult.Outcome, roomCode);

        if (joinResult.Outcome is JoinOutcome.Joined)
        {
            await relayService.RelayToPeerAsync(
                roomCode, deviceId, BuildEnvelope(SignalingMessageTypes.PeerJoined, roomCode, deviceId), ct);
        }

        try
        {
            await ReceiveLoopAsync(socket, roomCode, deviceId, ct);
        }
        finally
        {
            connectionRegistry.Unregister(roomCode, deviceId);
            roomService.Leave(roomCode, deviceId);
            await relayService.RelayToPeerAsync(
                roomCode, deviceId, BuildEnvelope(SignalingMessageTypes.PeerLeft, roomCode, deviceId), CancellationToken.None);
            logger.LogInformation("Device {DeviceId} left room {RoomCode}", deviceId, roomCode);
        }
    }

    private async Task ReceiveLoopAsync(WebSocket socket, string roomCode, string deviceId, CancellationToken ct)
    {
        var buffer = new byte[8 * 1024];

        while (socket.State == WebSocketState.Open)
        {
            using var messageStream = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, ct);
                    return;
                }

                messageStream.Write(buffer, 0, result.Count);
                if (messageStream.Length > MaxMessageBytes)
                {
                    await CloseAsync(socket, WebSocketCloseStatus.MessageTooBig, "message-too-large", ct);
                    return;
                }
            } while (!result.EndOfMessage);

            if (messageStream.Length == 0)
            {
                continue;
            }

            await HandleFrameAsync(messageStream.ToArray(), roomCode, deviceId, ct);
        }
    }

    private async Task HandleFrameAsync(byte[] rawBytes, string roomCode, string deviceId, CancellationToken ct)
    {
        SignalingMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<SignalingMessage>(rawBytes);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Malformed signaling frame from {DeviceId} in {RoomCode}", deviceId, roomCode);
            return;
        }

        if (message is null)
        {
            return;
        }

        switch (message.Type)
        {
            case SignalingMessageTypes.Offer:
            case SignalingMessageTypes.Answer:
            case SignalingMessageTypes.IceCandidate:
                await relayService.RelayToPeerAsync(roomCode, deviceId, Encoding.UTF8.GetString(rawBytes), ct);
                break;

            case SignalingMessageTypes.Bye:
                await relayService.RelayToPeerAsync(
                    roomCode, deviceId, BuildEnvelope(SignalingMessageTypes.Bye, roomCode, deviceId), ct);
                break;

            default:
                logger.LogWarning("Unknown signaling message type '{Type}' from {DeviceId}", message.Type, deviceId);
                break;
        }
    }

    private static async Task CloseAsync(WebSocket socket, WebSocketCloseStatus status, string reason, CancellationToken ct)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(status, reason, ct);
        }
    }

    private static string BuildEnvelope(string type, string roomCode, string senderId) =>
        JsonSerializer.Serialize(new SignalingMessage { Type = type, RoomCode = roomCode, SenderId = senderId });
}

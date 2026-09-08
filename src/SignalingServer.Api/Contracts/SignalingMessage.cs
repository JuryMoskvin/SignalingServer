using System.Text.Json;
using System.Text.Json.Serialization;

namespace SignalingServer.Api.Contracts;

/// <summary>
/// Wire format for every WS frame. The server only ever reads <see cref="Type"/> and
/// <see cref="SenderId"/> to route the message — <see cref="Payload"/> (the SDP/ICE
/// body) is forwarded to the peer byte-for-byte, never parsed or interpreted.
/// </summary>
public sealed class SignalingMessage
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("roomCode")]
    public string? RoomCode { get; init; }

    [JsonPropertyName("senderId")]
    public string? SenderId { get; init; }

    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; init; }
}

public static class SignalingMessageTypes
{
    public const string Join = "join";
    public const string Offer = "offer";
    public const string Answer = "answer";
    public const string IceCandidate = "ice-candidate";
    public const string PeerJoined = "peer-joined";
    public const string PeerLeft = "peer-left";
    public const string Bye = "bye";
    public const string Error = "error";
    public const string RoomClosed = "room-closed";
}

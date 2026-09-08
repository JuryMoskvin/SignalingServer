namespace SignalingServer.Application.Signaling;

/// <summary>
/// Forwards signaling payloads (offer/answer/ice-candidate) verbatim between the two
/// participants of a room. The server never inspects or parses the SDP/ICE content.
/// </summary>
public interface ISignalingRelayService
{
    Task<bool> RelayToPeerAsync(string roomCode, string senderDeviceId, string rawJsonMessage, CancellationToken ct);
}

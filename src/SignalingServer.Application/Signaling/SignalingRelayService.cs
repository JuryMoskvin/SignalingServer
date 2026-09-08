using SignalingServer.Application.Rooms;

namespace SignalingServer.Application.Signaling;

public sealed class SignalingRelayService(IRoomService roomService, IConnectionRegistry registry)
    : ISignalingRelayService
{
    public async Task<bool> RelayToPeerAsync(string roomCode, string senderDeviceId, string rawJsonMessage, CancellationToken ct)
    {
        var room = roomService.GetRoom(roomCode);
        var peerId = room?.Participants.FirstOrDefault(p => p.DeviceId != senderDeviceId)?.DeviceId;
        if (peerId is null)
        {
            return false;
        }

        return await registry.SendAsync(roomCode, peerId, rawJsonMessage, ct);
    }
}

namespace SignalingServer.Application.Rooms;

public interface IRoomService
{
    Room CreateRoom();

    Room? GetRoom(string code);

    JoinResult TryJoin(string code, string deviceId);

    void Leave(string code, string deviceId);

    /// <summary>Evicts expired/empty rooms. Called periodically by the cleanup background service.</summary>
    int EvictExpired();
}

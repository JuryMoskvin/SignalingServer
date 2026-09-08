using System.Collections.Concurrent;

namespace SignalingServer.Application.Rooms;

/// <summary>
/// Single-process, in-memory room store. Sufficient for a single-instance deployment.
/// Swap for a Redis-backed implementation of <see cref="IRoomRepository"/> when scaling
/// horizontally across multiple instances (see architecture notes on TURN/scaling readiness).
/// </summary>
public sealed class InMemoryRoomRepository : IRoomRepository
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();

    public bool TryAdd(Room room) => _rooms.TryAdd(room.Code, room);

    public Room? Get(string code) => _rooms.GetValueOrDefault(code);

    public bool Remove(string code) => _rooms.TryRemove(code, out _);

    public IReadOnlyCollection<Room> GetAll() => _rooms.Values.ToArray();
}

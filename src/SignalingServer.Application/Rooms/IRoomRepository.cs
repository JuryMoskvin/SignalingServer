namespace SignalingServer.Application.Rooms;

public interface IRoomRepository
{
    bool TryAdd(Room room);

    Room? Get(string code);

    bool Remove(string code);

    IReadOnlyCollection<Room> GetAll();
}

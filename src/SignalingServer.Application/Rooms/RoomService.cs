namespace SignalingServer.Application.Rooms;

public sealed class RoomService : IRoomService
{
    private readonly IRoomRepository _repository;
    private readonly RoomCodeGenerator _codeGenerator;
    private readonly RoomOptions _options;
    private readonly TimeProvider _timeProvider;

    public RoomService(
        IRoomRepository repository,
        RoomCodeGenerator codeGenerator,
        RoomOptions options,
        TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _codeGenerator = codeGenerator;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Room CreateRoom()
    {
        var now = _timeProvider.GetUtcNow();
        var room = new Room
        {
            Code = _codeGenerator.GenerateUnique(),
            CreatedAtUtc = now,
            ExpiresAtUtc = now + _options.WaitingTtl,
            Status = RoomStatus.Waiting
        };

        if (!_repository.TryAdd(room))
        {
            throw new InvalidOperationException("Room code collision, please retry.");
        }

        return room;
    }

    public Room? GetRoom(string code) => _repository.Get(code);

    public JoinResult TryJoin(string code, string deviceId)
    {
        var room = _repository.Get(code);
        if (room is null || room.Status == RoomStatus.Closed)
        {
            return new JoinResult(JoinOutcome.RoomNotFound, null);
        }

        lock (room)
        {
            var now = _timeProvider.GetUtcNow();
            var existing = room.Participants.FirstOrDefault(p => p.DeviceId == deviceId);
            if (existing is not null)
            {
                existing.DisconnectedAtUtc = null;
                return new JoinResult(JoinOutcome.Rejoined, room);
            }

            if (room.IsFull)
            {
                return new JoinResult(JoinOutcome.RoomFull, room);
            }

            room.Participants.Add(new Participant(deviceId, now));
            room.Status = room.ConnectedCount >= Room.MaxParticipants ? RoomStatus.Active : RoomStatus.Waiting;
            room.ExpiresAtUtc = now + _options.MaxRoomLifetime;

            return new JoinResult(JoinOutcome.Joined, room);
        }
    }

    public void Leave(string code, string deviceId)
    {
        var room = _repository.Get(code);
        if (room is null)
        {
            return;
        }

        lock (room)
        {
            var participant = room.Participants.FirstOrDefault(p => p.DeviceId == deviceId);
            if (participant is null)
            {
                return;
            }

            participant.DisconnectedAtUtc = _timeProvider.GetUtcNow();

            if (room.ConnectedCount == 0)
            {
                room.ExpiresAtUtc = _timeProvider.GetUtcNow() + _options.EmptyGracePeriod;
            }
        }
    }

    public int EvictExpired()
    {
        var now = _timeProvider.GetUtcNow();
        var evicted = 0;

        foreach (var room in _repository.GetAll())
        {
            bool shouldEvict;
            lock (room)
            {
                shouldEvict = (now >= room.ExpiresAtUtc && room.ConnectedCount == 0)
                    || now >= room.CreatedAtUtc + _options.MaxRoomLifetime;

                if (shouldEvict)
                {
                    room.Status = RoomStatus.Closed;
                }
            }

            if (shouldEvict && _repository.Remove(room.Code))
            {
                evicted++;
            }
        }

        return evicted;
    }
}

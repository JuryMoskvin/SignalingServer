using SignalingServer.Application.Rooms;
using SignalingServer.Tests.TestSupport;

namespace SignalingServer.Tests.Rooms;

public class RoomServiceTests
{
    private static RoomService CreateService(RoomOptions? options = null, FakeTimeProvider? timeProvider = null)
    {
        var opts = options ?? new RoomOptions();
        var repository = new InMemoryRoomRepository();
        var generator = new RoomCodeGenerator(repository, opts);
        return new RoomService(repository, generator, opts, timeProvider ?? new FakeTimeProvider());
    }

    [Fact]
    public void CreateRoom_StartsWaitingWithNoParticipants()
    {
        var service = CreateService();

        var room = service.CreateRoom();

        Assert.Equal(RoomStatus.Waiting, room.Status);
        Assert.Empty(room.Participants);
    }

    [Fact]
    public void TryJoin_FirstParticipant_KeepsRoomWaiting()
    {
        var service = CreateService();
        var room = service.CreateRoom();

        var result = service.TryJoin(room.Code, "device-a");

        Assert.Equal(JoinOutcome.Joined, result.Outcome);
        Assert.Equal(RoomStatus.Waiting, result.Room!.Status);
    }

    [Fact]
    public void TryJoin_SecondParticipant_ActivatesRoom()
    {
        var service = CreateService();
        var room = service.CreateRoom();
        service.TryJoin(room.Code, "device-a");

        var result = service.TryJoin(room.Code, "device-b");

        Assert.Equal(JoinOutcome.Joined, result.Outcome);
        Assert.Equal(RoomStatus.Active, result.Room!.Status);
    }

    [Fact]
    public void TryJoin_ThirdParticipant_IsRejected()
    {
        var service = CreateService();
        var room = service.CreateRoom();
        service.TryJoin(room.Code, "device-a");
        service.TryJoin(room.Code, "device-b");

        var result = service.TryJoin(room.Code, "device-c");

        Assert.Equal(JoinOutcome.RoomFull, result.Outcome);
    }

    [Fact]
    public void TryJoin_UnknownCode_ReturnsRoomNotFound()
    {
        var service = CreateService();

        var result = service.TryJoin("ZZZZZZ", "device-a");

        Assert.Equal(JoinOutcome.RoomNotFound, result.Outcome);
    }

    [Fact]
    public void TryJoin_SameDeviceReconnecting_ReturnsRejoinedAndClearsDisconnect()
    {
        var service = CreateService();
        var room = service.CreateRoom();
        service.TryJoin(room.Code, "device-a");
        service.Leave(room.Code, "device-a");

        var result = service.TryJoin(room.Code, "device-a");

        Assert.Equal(JoinOutcome.Rejoined, result.Outcome);
        Assert.True(result.Room!.Participants.Single().IsConnected);
    }

    [Fact]
    public void Leave_LastParticipant_SchedulesGraceExpiry()
    {
        var timeProvider = new FakeTimeProvider();
        var options = new RoomOptions { EmptyGracePeriod = TimeSpan.FromSeconds(30) };
        var service = CreateService(options, timeProvider);
        var room = service.CreateRoom();
        service.TryJoin(room.Code, "device-a");

        service.Leave(room.Code, "device-a");

        Assert.Equal(timeProvider.GetUtcNow() + options.EmptyGracePeriod, room.ExpiresAtUtc);
    }

    [Fact]
    public void EvictExpired_RemovesEmptyRoomPastGracePeriod()
    {
        var timeProvider = new FakeTimeProvider();
        var options = new RoomOptions { EmptyGracePeriod = TimeSpan.FromSeconds(30) };
        var service = CreateService(options, timeProvider);
        var room = service.CreateRoom();
        service.TryJoin(room.Code, "device-a");
        service.Leave(room.Code, "device-a");

        timeProvider.UtcNow += TimeSpan.FromSeconds(31);
        var evicted = service.EvictExpired();

        Assert.Equal(1, evicted);
        Assert.Null(service.GetRoom(room.Code));
    }

    [Fact]
    public void EvictExpired_KeepsRoomWithConnectedParticipants()
    {
        var timeProvider = new FakeTimeProvider();
        var options = new RoomOptions { WaitingTtl = TimeSpan.FromSeconds(1) };
        var service = CreateService(options, timeProvider);
        var room = service.CreateRoom();
        service.TryJoin(room.Code, "device-a");

        timeProvider.UtcNow += TimeSpan.FromMinutes(5);
        var evicted = service.EvictExpired();

        Assert.Equal(0, evicted);
        Assert.NotNull(service.GetRoom(room.Code));
    }
}

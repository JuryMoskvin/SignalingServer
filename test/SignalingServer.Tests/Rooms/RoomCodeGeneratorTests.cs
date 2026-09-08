using SignalingServer.Application.Rooms;

namespace SignalingServer.Tests.Rooms;

public class RoomCodeGeneratorTests
{
    private static readonly RoomOptions DefaultOptions = new() { CodeLength = 6 };
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    [Fact]
    public void GenerateUnique_ReturnsCodeOfConfiguredLength()
    {
        var generator = new RoomCodeGenerator(new InMemoryRoomRepository(), DefaultOptions);

        var code = generator.GenerateUnique();

        Assert.Equal(DefaultOptions.CodeLength, code.Length);
    }

    [Fact]
    public void GenerateUnique_UsesOnlyUnambiguousAlphabet()
    {
        var generator = new RoomCodeGenerator(new InMemoryRoomRepository(), DefaultOptions);

        for (var i = 0; i < 50; i++)
        {
            var code = generator.GenerateUnique();
            Assert.All(code, c => Assert.Contains(c, Alphabet));
        }
    }

    [Fact]
    public void GenerateUnique_ThrowsAfterRepeatedCollisions()
    {
        var generator = new RoomCodeGenerator(new AlwaysOccupiedRoomRepository(), DefaultOptions);

        Assert.Throws<InvalidOperationException>(() => generator.GenerateUnique());
    }

    private sealed class AlwaysOccupiedRoomRepository : IRoomRepository
    {
        public bool TryAdd(Room room) => false;

        public Room? Get(string code) => new() { Code = code, CreatedAtUtc = DateTimeOffset.UtcNow };

        public bool Remove(string code) => false;

        public IReadOnlyCollection<Room> GetAll() => [];
    }
}

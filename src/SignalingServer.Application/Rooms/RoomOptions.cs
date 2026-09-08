namespace SignalingServer.Application.Rooms;

public sealed class RoomOptions
{
    public const string SectionName = "Rooms";

    /// <summary>Length of the human-shareable room code (6-8 recommended).</summary>
    public int CodeLength { get; set; } = 6;

    /// <summary>How long a room may stay in Waiting status (created, second peer not yet joined) before eviction.</summary>
    public TimeSpan WaitingTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Grace period after both participants disconnect before the room is evicted (survives brief reconnects).</summary>
    public TimeSpan EmptyGracePeriod { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Hard safety cap on how long any room may live regardless of activity.</summary>
    public TimeSpan MaxRoomLifetime { get; set; } = TimeSpan.FromHours(2);
}

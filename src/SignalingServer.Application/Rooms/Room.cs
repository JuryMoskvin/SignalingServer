namespace SignalingServer.Application.Rooms;

public sealed class Room
{
    public const int MaxParticipants = 2;

    public required string Code { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Waiting;
    public List<Participant> Participants { get; } = new(capacity: MaxParticipants);

    public int ConnectedCount => Participants.Count(p => p.IsConnected);

    public bool IsFull => ConnectedCount >= MaxParticipants;
}

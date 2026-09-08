namespace SignalingServer.Application.Rooms;

public sealed record Participant(string DeviceId, DateTimeOffset JoinedAtUtc)
{
    public DateTimeOffset? DisconnectedAtUtc { get; set; }

    public bool IsConnected => DisconnectedAtUtc is null;
}

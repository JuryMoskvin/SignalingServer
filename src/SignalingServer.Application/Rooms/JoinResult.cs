namespace SignalingServer.Application.Rooms;

public enum JoinOutcome
{
    Joined,
    Rejoined,
    RoomNotFound,
    RoomFull
}

public readonly record struct JoinResult(JoinOutcome Outcome, Room? Room);

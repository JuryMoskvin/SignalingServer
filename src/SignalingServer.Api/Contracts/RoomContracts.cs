namespace SignalingServer.Api.Contracts;

public sealed record CreateRoomResponse(string RoomCode, DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc);

public sealed record RoomInfoResponse(string RoomCode, string Status, int ParticipantsCount);

public sealed record IceServerDto(string Urls, string? Username = null, string? Credential = null);

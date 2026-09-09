using SignalingServer.Api.Contracts;
using SignalingServer.Api.Options;
using SignalingServer.Application.Rooms;

namespace SignalingServer.Api.Endpoints;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ws/api/rooms").WithTags("Rooms");

        group.MapPost("/", (IRoomService roomService) =>
        {
            var room = roomService.CreateRoom();
            return Results.Ok(new CreateRoomResponse(room.Code, room.CreatedAtUtc, room.ExpiresAtUtc));
        }).RequireRateLimiting(RateLimiterPolicies.RoomCreation);

        group.MapGet("/{code}", (string code, IRoomService roomService) =>
        {
            var room = roomService.GetRoom(code.ToUpperInvariant());
            if (room is null || room.Status == RoomStatus.Closed)
            {
                return Results.NotFound();
            }

            return Results.Ok(new RoomInfoResponse(
                room.Code,
                room.Status.ToString().ToLowerInvariant(),
                room.ConnectedCount));
        });

        return app;
    }
}

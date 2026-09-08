using SignalingServer.Api.WebSockets;
using SignalingServer.Application.Rooms;

namespace SignalingServer.Api.Endpoints;

public static class SignalingEndpoints
{
    public static IEndpointRouteBuilder MapSignalingEndpoints(this IEndpointRouteBuilder app)
    {
        app.Map("/ws/signaling", async (HttpContext context, SignalingConnectionHandler handler, IRoomService roomService) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var roomCode = context.Request.Query["roomCode"].ToString().ToUpperInvariant();
            var deviceId = context.Request.Query["deviceId"].ToString();

            if (string.IsNullOrWhiteSpace(roomCode) || string.IsNullOrWhiteSpace(deviceId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (roomService.GetRoom(roomCode) is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await handler.HandleAsync(socket, roomCode, deviceId, context.RequestAborted);
        });

        return app;
    }
}

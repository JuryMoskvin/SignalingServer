using Microsoft.Extensions.Options;
using SignalingServer.Api.Contracts;
using SignalingServer.Api.Options;

namespace SignalingServer.Api.Endpoints;

public static class IceServersEndpoints
{
    public static IEndpointRouteBuilder MapIceServersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/ws/api/ice-servers", (IOptions<IceServersOptions> iceOptions, IOptions<TurnOptions> turnOptions) =>
        {
            var servers = new List<IceServerDto>(iceOptions.Value.Servers);

            var turn = turnOptions.Value;
            if (turn.IsConfigured)
            {
                // Both transports: UDP is preferred (lower overhead) but TCP is the
                // fallback for networks that block/throttle UDP outright.
                servers.Add(new IceServerDto($"turn:{turn.Host}:{turn.Port}", turn.Username, turn.Password));
                servers.Add(new IceServerDto($"turn:{turn.Host}:{turn.Port}?transport=tcp", turn.Username, turn.Password));
            }

            return Results.Ok(servers);
        }).WithTags("IceServers");

        return app;
    }
}

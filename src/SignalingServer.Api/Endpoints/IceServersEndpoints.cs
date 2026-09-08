using Microsoft.Extensions.Options;
using SignalingServer.Api.Options;

namespace SignalingServer.Api.Endpoints;

public static class IceServersEndpoints
{
    public static IEndpointRouteBuilder MapIceServersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ice-servers", (IOptions<IceServersOptions> options) => Results.Ok(options.Value.Servers))
            .WithTags("IceServers");

        return app;
    }
}

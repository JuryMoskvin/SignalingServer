using Microsoft.AspNetCore.RateLimiting;
using SignalingServer.Api.Endpoints;
using SignalingServer.Api.Options;
using SignalingServer.Api.WebSockets;
using SignalingServer.Application.BackgroundServices;
using SignalingServer.Application.Rooms;
using SignalingServer.Application.Signaling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<IceServersOptions>(builder.Configuration.GetSection(IceServersOptions.SectionName));

var roomOptions = builder.Configuration.GetSection(RoomOptions.SectionName).Get<RoomOptions>() ?? new RoomOptions();
builder.Services.AddSingleton(roomOptions);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRoomRepository, InMemoryRoomRepository>();
builder.Services.AddSingleton<RoomCodeGenerator>();
builder.Services.AddSingleton<IRoomService, RoomService>();
builder.Services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();
builder.Services.AddSingleton<ISignalingRelayService, SignalingRelayService>();
builder.Services.AddSingleton<SignalingConnectionHandler>();
builder.Services.AddHostedService<RoomCleanupService>();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(RateLimiterPolicies.RoomCreation, limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseRateLimiter();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapRoomEndpoints();
app.MapIceServersEndpoints();
app.MapSignalingEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;

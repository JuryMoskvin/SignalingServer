using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SignalingServer.Application.Rooms;

namespace SignalingServer.Application.BackgroundServices;

public sealed class RoomCleanupService(IRoomService roomService, ILogger<RoomCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var evicted = roomService.EvictExpired();
                if (evicted > 0)
                {
                    logger.LogInformation("Evicted {Count} expired/empty room(s)", evicted);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Room cleanup sweep failed");
            }
        }
    }
}

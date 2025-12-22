using BitNow_Backend.DAL.IRepositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.Services
{
    public class AuctionStatusUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuctionStatusUpdateService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(15); // Update every 15 seconds for faster sync

        public AuctionStatusUpdateService(
            IServiceProvider serviceProvider,
            ILogger<AuctionStatusUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AuctionStatusUpdateService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var auctionRepository = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();

                        // Update scheduled auctions to active
                        var scheduledToActiveCount = await auctionRepository.UpdateScheduledToActiveAsync();
                        if (scheduledToActiveCount > 0)
                        {
                            _logger.LogInformation($"Updated {scheduledToActiveCount} auction(s) from scheduled to active.");
                        }

                        // Update active auctions to completed
                        var activeToCompletedCount = await auctionRepository.UpdateActiveToCompletedAsync();
                        if (activeToCompletedCount > 0)
                        {
                            _logger.LogInformation($"Updated {activeToCompletedCount} auction(s) from active to completed.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating auction statuses.");
                }

                // Wait for the next update interval
                await Task.Delay(_updateInterval, stoppingToken);
            }

            _logger.LogInformation("AuctionStatusUpdateService is stopping.");
        }
    }
}



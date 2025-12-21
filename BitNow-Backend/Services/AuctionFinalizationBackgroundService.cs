using BitNow_Backend.BLL.IServices;
using BitNow_Backend.RealTime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BitNow_Backend.Services
{
    public class AuctionFinalizationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<AuctionHub> _hubContext;
        private readonly ILogger<AuctionFinalizationBackgroundService> _logger;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

        public AuctionFinalizationBackgroundService(
            IServiceScopeFactory scopeFactory,
            IHubContext<AuctionHub> hubContext,
            ILogger<AuctionFinalizationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 AuctionFinalizationBackgroundService started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();
                    var completions = await auctionService.FinalizeExpiredAuctionsAsync(stoppingToken);

                    if (completions.Count > 0)
                    {
                        _logger.LogInformation("✅ Finalized {Count} expired auctions", completions.Count);
                        
                        foreach (var completion in completions)
                        {
                            var payload = new
                            {
                                auctionId = completion.AuctionId,
                                status = completion.Status,
                                winnerId = completion.WinnerId,
                                finalPrice = completion.FinalPrice,
                                completionType = completion.CompletionType,
                                timestamp = completion.CompletedAt
                            };

                            _logger.LogInformation("📢 Broadcasting AuctionStatusUpdated for auction {AuctionId}, winnerId={WinnerId}", 
                                completion.AuctionId, completion.WinnerId);

                            await _hubContext.Clients.Group($"auction-{completion.AuctionId}")
                                .SendAsync("AuctionStatusUpdated", payload, stoppingToken);
                            await _hubContext.Clients.Group(AuctionHub.AdminAuctionsGroup)
                                .SendAsync("AdminAuctionStatusUpdated", payload, stoppingToken);
                            await _hubContext.Clients.Group(AuctionHub.AdminDashboardGroup)
                                .SendAsync("AdminStatsUpdated", stoppingToken);
                            await _hubContext.Clients.Group(AuctionHub.AdminAnalyticsGroup)
                                .SendAsync("AdminAnalyticsUpdated", stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 AuctionFinalizationBackgroundService is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Lỗi khi tự động cập nhật trạng thái phiên đấu giá hết hạn: {Message}", ex.Message);
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            
            _logger.LogInformation("🛑 AuctionFinalizationBackgroundService stopped");
        }
    }
}



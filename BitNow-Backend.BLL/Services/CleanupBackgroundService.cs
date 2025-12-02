using BitNow_Backend.BLL.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.BackgroundServices
{

    public class CleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CleanupBackgroundService> _logger;

        // Hard-coded settings
        private static readonly TimeSpan CLEANUP_INTERVAL = TimeSpan.FromHours(24);
        private static readonly TimeSpan KEYWORD_RETENTION = TimeSpan.FromDays(180); // 6 tháng
        private static readonly TimeSpan STARTUP_DELAY = TimeSpan.FromMinutes(1);

        public CleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<CleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(" CleanupBackgroundService started");
            _logger.LogInformation(" Schedule: Every 24h | Keywords retention: 180 days");

            try
            {
                // Đợi 1 phút sau khi app khởi động
                _logger.LogInformation(" Waiting {Delay} before first cleanup...", STARTUP_DELAY);
                await Task.Delay(STARTUP_DELAY, stoppingToken);

                // Chạy cleanup ngay lần đầu
                await RunCleanupTasksAsync(stoppingToken);

                // Sau đó chạy mỗi 24h
                using var timer = new PeriodicTimer(CLEANUP_INTERVAL);

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RunCleanupTasksAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("CleanupBackgroundService is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Fatal error in CleanupBackgroundService");
                throw;
            }
        }

        private async Task RunCleanupTasksAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("===  Starting cleanup tasks ===");
            var startTime = DateTime.UtcNow;

            // Chạy song song 2 tasks
            var auctionTask = CleanupExpiredAuctionsAsync(stoppingToken);
            var keywordTask = CleanupOldSearchKeywordsAsync(stoppingToken);

            await Task.WhenAll(auctionTask, keywordTask);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("===  Cleanup completed in {Duration:F2}s ===", duration.TotalSeconds);
        }


        /// Xóa expired auctions từ Pinecone

        private async Task CleanupExpiredAuctionsAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("[Auction]  Scanning for expired auctions...");
                var startTime = DateTime.UtcNow;

                using var scope = _serviceProvider.CreateScope();
                var vectorSyncService = scope.ServiceProvider.GetRequiredService<IVectorSyncService>();

                await vectorSyncService.RemoveExpiredAuctionsAsync(stoppingToken);

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("[Auction]  Completed in {Duration:F2}s", duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Auction]  Failed: {Message}", ex.Message);
                // Không throw để service tiếp tục chạy
            }
        }

        /// Xóa search keywords cũ hơn 6 tháng

        private async Task CleanupOldSearchKeywordsAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("[Keyword]  Scanning for keywords older than {Days} days...",
                    KEYWORD_RETENTION.TotalDays);
                var startTime = DateTime.UtcNow;

                using var scope = _serviceProvider.CreateScope();
                var searchKeywordService = scope.ServiceProvider.GetRequiredService<ISearchKeywordService>();

                var deletedCount = await searchKeywordService.DeleteOldKeywordsAsync(
                    KEYWORD_RETENTION,
                    stoppingToken);

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("[Keyword]  Deleted {Count} old keywords in {Duration:F2}s",
                    deletedCount, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Keyword] ❌ Failed: {Message}", ex.Message);
                // Không throw để service tiếp tục chạy
            }
        }

    }
}
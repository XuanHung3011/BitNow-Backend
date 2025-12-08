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

        private static readonly TimeSpan KEYWORD_RETENTION = TimeSpan.FromDays(180); // 6 tháng
        private static readonly TimeSpan STARTUP_DELAY = TimeSpan.FromMinutes(1);
        private static readonly int CLEANUP_HOUR = 1; // 1h sáng
        private static readonly TimeZoneInfo VN_TIMEZONE = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

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
            _logger.LogInformation(" Schedule: Daily at 1:00 AM (Vietnam Time) | Keywords retention: 180 days");

            try
            {
                // Đợi 1 phút sau khi app khởi động
                _logger.LogInformation(" Waiting {Delay} before first cleanup...", STARTUP_DELAY);
                await Task.Delay(STARTUP_DELAY, stoppingToken);

                // Chạy cleanup ngay lần đầu
                await RunCleanupTasksAsync(stoppingToken);

                // Sau đó chạy vào 1h sáng mỗi ngày
                while (!stoppingToken.IsCancellationRequested)
                {
                    var delay = CalculateDelayUntilNextRun();
                    _logger.LogInformation(" Next cleanup scheduled in {Hours:F1} hours at {NextRun}",
                        delay.TotalHours,
                        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Add(delay), VN_TIMEZONE).ToString("yyyy-MM-dd HH:mm:ss"));

                    await Task.Delay(delay, stoppingToken);
                    await RunCleanupTasksAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("CleanupBackgroundService is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fatal error in CleanupBackgroundService");
                throw;
            }
        }

        private TimeSpan CalculateDelayUntilNextRun()
        {
            var nowUtc = DateTime.UtcNow;
            var nowVN = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, VN_TIMEZONE);

            // Tính thời điểm 1h sáng hôm nay (VN time)
            var nextRunVN = new DateTime(nowVN.Year, nowVN.Month, nowVN.Day, CLEANUP_HOUR, 0, 0);

            // Nếu đã qua 1h sáng hôm nay, chuyển sang 1h sáng ngày mai
            if (nowVN >= nextRunVN)
            {
                nextRunVN = nextRunVN.AddDays(1);
            }

            // Chuyển về UTC để tính delay
            var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunVN, VN_TIMEZONE);
            var delay = nextRunUtc - nowUtc;

            return delay;
        }

        private async Task RunCleanupTasksAsync(CancellationToken stoppingToken)
        {
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VN_TIMEZONE);
            _logger.LogInformation("===  Starting cleanup tasks at {VNTime} (VN) ===", vnTime.ToString("yyyy-MM-dd HH:mm:ss"));
            var startTime = DateTime.UtcNow;

            // Chạy song song 2 tasks
            var auctionTask = CleanupExpiredAuctionsAsync(stoppingToken);
            var keywordTask = CleanupOldSearchKeywordsAsync(stoppingToken);

            await Task.WhenAll(auctionTask, keywordTask);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("===  Cleanup completed in {Duration:F2}s ===", duration.TotalSeconds);
        }

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
            }
        }

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
                _logger.LogError(ex, "[Keyword]  Failed: {Message}", ex.Message);
            }
        }
    }
}
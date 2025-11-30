using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Services;

public class SearchKeywordService : ISearchKeywordService
{
    private readonly ISearchKeywordRepository _searchKeywordRepository;
    private readonly ILogger<SearchKeywordService> _logger;

    public SearchKeywordService(
         ISearchKeywordRepository searchKeywordRepository,
         ILogger<SearchKeywordService> logger)
    {
        _searchKeywordRepository = searchKeywordRepository;
        _logger = logger;
    }

    public async Task LogSearchAsync(int userId, string keyword)
    {
        if (userId <= 0) return;
        if (string.IsNullOrWhiteSpace(keyword)) return;

        var trimmed = keyword.Trim();
        if (trimmed.Length == 0) return;

        var entity = new SearchKeyword
        {
            UserId = userId,
            Keyword = trimmed,
            CreatedAt = DateTime.UtcNow
        };

        await _searchKeywordRepository.AddAsync(entity);
    }

    public async Task<IReadOnlyList<string>> GetRecentKeywordsAsync(int userId, int take = 20)
    {
        if (userId <= 0) return Array.Empty<string>();
        if (take <= 0) take = 20;

        var entities = await _searchKeywordRepository.GetRecentByUserAsync(userId, take);

        // Lọc trùng và rỗng
        var keywords = entities
            .Select(k => k.Keyword?.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return keywords;
    }


    /// Xóa các search keywords cũ hơn retention period 
    public async Task<int> DeleteOldKeywordsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);
            _logger.LogInformation("Deleting search keywords older than {CutoffDate} ({Days} days)",
                cutoffDate, retentionPeriod.TotalDays);

            var deletedCount = await _searchKeywordRepository.DeleteOlderThanAsync(cutoffDate, cancellationToken);

            _logger.LogInformation("Successfully deleted {Count} old search keywords", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old search keywords");
            throw;
        }
    }
}

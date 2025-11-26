using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitNow_Backend.BLL.Services;

public class SearchKeywordService : ISearchKeywordService
{
    private readonly ISearchKeywordRepository _searchKeywordRepository;

    public SearchKeywordService(ISearchKeywordRepository searchKeywordRepository)
    {
        _searchKeywordRepository = searchKeywordRepository;
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
}

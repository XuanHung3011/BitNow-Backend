using BitNow_Backend.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.DAL.IRepositories
{
    public interface ISearchKeywordRepository
    {
        Task AddAsync(SearchKeyword keyword);

        /// Lấy danh sách các từ khóa tìm kiếm gần đây của một user.
        Task<List<SearchKeyword>> GetRecentByUserAsync(int userId, int take = 20);
        /// Xóa keywords cũ hơn ngày cutoff.
        Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    }
}

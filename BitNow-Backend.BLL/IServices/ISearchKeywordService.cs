using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitNow_Backend.BLL.IServices;

public interface ISearchKeywordService
{

    /// Ghi lại từ khóa tìm kiếm của user nếu hợp lệ.
    Task LogSearchAsync(int userId, string keyword);


    /// Lấy các từ khóa tìm kiếm gần đây của user.
    Task<IReadOnlyList<string>> GetRecentKeywordsAsync(int userId, int take = 20);

    /// Xóa các keywords cũ hơn retention period
    Task<int> DeleteOldKeywordsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}


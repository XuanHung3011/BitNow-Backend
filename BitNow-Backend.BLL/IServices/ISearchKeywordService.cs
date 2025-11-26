using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitNow_Backend.BLL.IServices;

public interface ISearchKeywordService
{
    /// <summary>
    /// Ghi lại từ khóa tìm kiếm của user nếu hợp lệ.
    /// </summary>
    /// <param name="userId">Id người dùng (đã đăng nhập).</param>
    /// <param name="keyword">Từ khóa tìm kiếm.</param>
    Task LogSearchAsync(int userId, string keyword);

    /// <summary>
    /// Lấy các từ khóa tìm kiếm gần đây của user.
    /// </summary>
    /// <param name="userId">Id người dùng.</param>
    /// <param name="take">Số lượng từ khóa muốn lấy.</param>
    Task<IReadOnlyList<string>> GetRecentKeywordsAsync(int userId, int take = 20);
}


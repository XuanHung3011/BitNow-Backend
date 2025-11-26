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
}



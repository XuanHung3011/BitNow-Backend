using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BitNow_Backend.BLL.IServices
{
    public interface IUserAuctionViewService
    {

        /// Ghi lại lượt xem auction của user, chỉ lưu nếu cách lần gần nhất ít nhất intervalMinutes phút.
        Task LogViewAsync(int userId, int auctionId, int intervalMinutes = 3, CancellationToken cancellationToken = default);


        /// Lấy danh sách auctionId mà user đã xem gần đây (debounced theo logic LogViewAsync).
        Task<IReadOnlyList<int>> GetRecentViewedAuctionIdsAsync(int userId, int take = 20, CancellationToken cancellationToken = default);
    }
}


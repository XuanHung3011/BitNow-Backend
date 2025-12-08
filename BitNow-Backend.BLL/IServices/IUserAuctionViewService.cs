using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BitNow_Backend.BLL.IServices
{
    public interface IUserAuctionViewService
    {

        
        Task LogViewAsync(int userId, int auctionId, int intervalMinutes = 3, CancellationToken cancellationToken = default);


        
        Task<IReadOnlyList<int>> GetRecentViewedAuctionIdsAsync(int userId, int take = 20, CancellationToken cancellationToken = default);

        Task<int> DeleteOldViewsAsync(TimeSpan retention, CancellationToken cancellationToken = default);
    }
}


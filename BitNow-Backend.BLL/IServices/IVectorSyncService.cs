using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
    /// <summary>
    /// Service để đồng bộ dữ liệu phiên đấu giá vào Pinecone vector database.
    /// </summary>
    public interface IVectorSyncService
    {
        /// <summary>
        /// Đồng bộ tất cả các phiên đấu giá đang active vào Pinecone.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SyncActiveAuctionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Đồng bộ một phiên đấu giá cụ thể vào Pinecone.
        /// </summary>
        /// <param name="item">Item cần đồng bộ</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SyncAuctionAsync(ItemResponseDto item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa các phiên đấu giá đã hết thời gian khỏi Pinecone.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RemoveExpiredAuctionsAsync(CancellationToken cancellationToken = default);

        Task ClearAllVectorsAsync(CancellationToken cancellationToken = default);
    }
}


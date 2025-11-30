using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{

    public interface IVectorSyncService
    {

        /// Tạo embedding vector từ văn bản sử dụng LM Studio (local) với Nomic Embed model.
        Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

  
        /// Đồng bộ tất cả các phiên đấu giá đang active vào Pinecone.
        Task SyncActiveAuctionsAsync(CancellationToken cancellationToken = default);


        /// Đồng bộ một phiên đấu giá cụ thể vào Pinecone.
        Task SyncAuctionAsync(ItemResponseDto item, CancellationToken cancellationToken = default);


        /// Xóa các phiên đấu giá đã hết thời gian khỏi Pinecone.
        Task RemoveExpiredAuctionsAsync(CancellationToken cancellationToken = default);


        /// Xóa tất cả vectors khỏi Pinecone index.
        Task ClearAllVectorsAsync(CancellationToken cancellationToken = default);
    }
}
namespace BitNow_Backend.BLL.IServices
{
    /// <summary>
    /// Service để tương tác với Pinecone vector database.
    /// </summary>
    public interface IPineconeService
    {
        /// <summary>
        /// Upsert vector vào Pinecone index.
        /// </summary>
        /// <param name="id">ID của vector (thường là auctionId hoặc itemId)</param>
        /// <param name="vector">Embedding vector</param>
        /// <param name="metadata">Metadata kèm theo (ví dụ: itemId, title, category, etc.)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UpsertVectorAsync(string id, float[] vector, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm kiếm các vector tương đồng nhất.
        /// </summary>
        /// <param name="queryVector">Vector query để tìm kiếm</param>
        /// <param name="topK">Số lượng kết quả muốn lấy</param>
        /// <param name="filter">Filter metadata (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Danh sách IDs và scores của các vector tương đồng</returns>
        Task<List<(string id, float score)>> QuerySimilarAsync(float[] queryVector, int topK = 4, Dictionary<string, object>? filter = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa vector khỏi Pinecone index.
        /// </summary>
        /// <param name="id">ID của vector cần xóa</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteVectorAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa nhiều vector cùng lúc.
        /// </summary>
        /// <param name="ids">Danh sách IDs cần xóa</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteVectorsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    }
}


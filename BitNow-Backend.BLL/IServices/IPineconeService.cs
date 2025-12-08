namespace BitNow_Backend.BLL.IServices
{

    public interface IPineconeService
    {

        /// Upsert vector vào Pinecone index.
        Task UpsertVectorAsync(string id, float[] vector, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm kiếm các vector tương đồng nhất.
        Task<List<(string id, float score)>> QuerySimilarAsync(float[] queryVector, int topK = 4, Dictionary<string, object>? filter = null, CancellationToken cancellationToken = default);


        /// Xóa vector khỏi Pinecone index.
        Task DeleteVectorAsync(string id, CancellationToken cancellationToken = default);


        /// Xóa nhiều vector cùng lúc.
        Task DeleteVectorsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);

        Task DeleteAllVectorsAsync(CancellationToken cancellationToken = default);
    }
}


namespace BitNow_Backend.BLL.IServices
{
    /// <summary>
    /// Service để tạo embedding vector từ văn bản sử dụng LM Studio (local) với Nomic Embed model.
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Chuyển đổi văn bản thành embedding vector.
        /// </summary>
        /// <param name="text">Văn bản cần chuyển đổi</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Mảng float chứa embedding vector</returns>
        Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    }
}


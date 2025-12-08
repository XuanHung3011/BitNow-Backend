using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
    public interface IRecommendationService
    {

        /// Trả về danh sách item gợi ý "Dành riêng cho bạn" cho người dùng.
        Task<IEnumerable<ItemResponseDto>> GetPersonalizedItemsAsync(int userId, int limit, CancellationToken cancellationToken = default);
    }
}

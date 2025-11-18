using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
    public interface IRecommendationService
    {
        /// <summary>
        /// Trả về danh sách item gợi ý "Dành riêng cho bạn" cho người dùng.
        /// Có thể sử dụng AI (OpenAI) kết hợp với dữ liệu lịch sử để sắp xếp.
        /// </summary>
        /// <param name="userId">Id người dùng (bidder)</param>
        /// <param name="limit">Số lượng item muốn lấy</param>
        Task<IEnumerable<ItemResponseDto>> GetPersonalizedItemsAsync(int userId, int limit, CancellationToken cancellationToken = default);
    }
}

using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.Payment;

public interface IOrderService
{
    /// <summary>
    /// Tạo order khi auction kết thúc cho winner
    /// </summary>
    Task<OrderDto> CreateOrderForWinnerAsync(int auctionId, int winnerId, decimal finalPrice);

    /// <summary>
    /// Lấy order theo ID
    /// </summary>
    Task<OrderDto?> GetOrderByIdAsync(int orderId);

    /// <summary>
    /// Lấy order theo auction ID
    /// </summary>
    Task<OrderDto?> GetOrderByAuctionIdAsync(int auctionId);

    /// <summary>
    /// Cập nhật order status
    /// </summary>
    Task<bool> UpdateOrderStatusAsync(int orderId, string status);

    /// <summary>
    /// Lấy orders của buyer
    /// </summary>
    Task<List<OrderDto>> GetOrdersByBuyerIdAsync(int buyerId);

    /// <summary>
    /// Lấy orders của seller
    /// </summary>
    Task<List<OrderDto>> GetOrdersBySellerIdAsync(int sellerId);

    /// <summary>
    /// Cập nhật thông tin vận chuyển cho order
    /// </summary>
    Task<bool> UpdateShippingInfoAsync(int orderId, string trackingNumber, string? shippingCompany, string? shippingAddress);
}


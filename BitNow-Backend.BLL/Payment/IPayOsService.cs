using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.Payment;

public interface IPayOsService
{
    /// <summary>
    /// Tạo payment link từ PayOS cho order
    /// </summary>
    Task<PayOsPaymentLinkDto> CreatePaymentLinkAsync(int orderId, decimal amount, string description, string returnUrl, string cancelUrl);

    /// <summary>
    /// Xác thực webhook từ PayOS
    /// </summary>
    bool VerifyWebhookSignature(string data, string signature);

    /// <summary>
    /// Xử lý webhook từ PayOS khi payment thành công/thất bại
    /// </summary>
    Task<PayOsWebhookResult> HandleWebhookAsync(PayOsWebhookDto webhookData);
}


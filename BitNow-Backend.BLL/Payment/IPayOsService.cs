using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.Payment;

public interface IPayOsService
{
    /// <summary>
    /// Tạo payment link từ PayOS
    /// </summary>
    Task<PayOsPaymentLinkDto> CreatePaymentLinkAsync(int orderId, decimal amount, string description, string returnUrl, string cancelUrl);

    /// <summary>
    /// Xác thực chữ ký webhook từ PayOS
    /// </summary>
    bool VerifyWebhookSignature(string data, string signature);

    /// <summary>
    /// Xử lý webhook từ PayOS
    /// </summary>
    Task<PayOsWebhookResult> HandleWebhookAsync(PayOsWebhookDto webhookData);

    /// <summary>
    /// Lấy thông tin payment từ PayOS bằng PaymentLinkId
    /// </summary>
    Task<PayOsPaymentLinkDto?> GetPaymentInformationAsync(string paymentLinkId);
}




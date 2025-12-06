using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.Payment;

public interface IPayOsService
{
    Task<PayOsPaymentLinkDto> CreatePaymentLinkAsync(int orderId, decimal amount, string description, string returnUrl, string cancelUrl);
    bool VerifyWebhookSignature(string data, string signature);
    Task<PayOsWebhookResult> HandleWebhookAsync(PayOsWebhookDto webhookData);
    Task<PayOsPaymentLinkDto?> GetPaymentInformationAsync(string paymentLinkId);
}






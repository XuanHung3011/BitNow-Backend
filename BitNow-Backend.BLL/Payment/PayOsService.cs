using Microsoft.Extensions.Configuration;
using BitNow_Backend.DAL.DTOs;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models.V2.PaymentRequests;

namespace BitNow_Backend.BLL.Payment;

public class PayOsService : IPayOsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<PayOsService> _logger;
    private readonly PayOSClient _payOSClient;

    public PayOsService(IConfiguration config, ILogger<PayOsService> logger)
    {
        _config = config;
        _logger = logger;
        
        // Initialize PayOS SDK client (like in official demo)
        _payOSClient = new PayOSClient(new PayOSOptions
        {
            ClientId = _config["PayOS:ClientId"] ?? "",
            ApiKey = _config["PayOS:ApiKey"] ?? "",
            ChecksumKey = _config["PayOS:ChecksumKey"] ?? "",
            LogLevel = LogLevel.Debug
        });
        
        _logger.LogInformation("PayOS SDK client initialized");
    }

    private string Shorten(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Order payment";

        input = input.Trim();
        // PayOS requires description max 25 characters - strictly enforce
        if (input.Length > 25)
        {
            input = input.Substring(0, 25);
        }
        // Remove any special characters that might cause issues
        return input;
    }

    public async Task<PayOsPaymentLinkDto> CreatePaymentLinkAsync(int orderId, decimal amount, string description, string returnUrl, string cancelUrl)
    {
        try
        {
            _logger.LogInformation("Creating PayOS payment link for order {OrderId}, amount={Amount}", orderId, amount);
            
            // Validate amount
            if (amount <= 0)
            {
                _logger.LogError("Invalid amount: {Amount}", amount);
                throw new InvalidOperationException($"Invalid amount: {amount}. Amount must be greater than 0.");
            }
            
            // Convert amount to long (VND) - PayOS requires amount in smallest unit (VND)
            var amountInVnd = (long)Math.Round(amount);
            
            if (amountInVnd <= 0)
            {
                _logger.LogError("Invalid amountInVnd: {AmountInVnd}", amountInVnd);
                throw new InvalidOperationException($"Invalid amount: {amountInVnd}. Amount must be greater than 0.");
            }
            
            // Generate orderCode using Unix timestamp (like in official demo)
            // PayOS requires orderCode >= 100000, Unix timestamp is always >= 100000
            var orderCode = DateTimeOffset.Now.ToUnixTimeSeconds();
            
            _logger.LogInformation("PayOS orderCode: {OrderCode} (generated from Unix timestamp)", orderCode);
            
            // Shorten description if needed
            var shortDescription = Shorten(description ?? $"Order {orderId}");
            
            // Create payment request using PayOS SDK (like in official demo)
            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amountInVnd,
                Description = shortDescription,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                Items = new List<PaymentLinkItem>
                {
                    new PaymentLinkItem
                    {
                        Name = shortDescription,
                        Quantity = 1,
                        Price = amountInVnd
                    }
                }
            };
            
            _logger.LogInformation("PayOS Request - OrderCode: {OrderCode}, Amount: {Amount}, Description: {Description}, ReturnUrl: {ReturnUrl}, CancelUrl: {CancelUrl}",
                orderCode, amountInVnd, shortDescription, returnUrl, cancelUrl);

            // Use PayOS SDK to create payment link (like in official demo)
            var paymentResponse = await _payOSClient.PaymentRequests.CreateAsync(paymentRequest);
            
            _logger.LogInformation("Payment link created successfully: {CheckoutUrl}, PaymentLinkId: {PaymentLinkId}", 
                paymentResponse.CheckoutUrl, paymentResponse.PaymentLinkId);

            return new PayOsPaymentLinkDto
            {
                OrderId = orderId,
                PaymentLink = paymentResponse.CheckoutUrl ?? "",
                PaymentLinkId = paymentResponse.PaymentLinkId ?? "",
                QrCode = paymentResponse.QrCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayOS payment link for order {OrderId}", orderId);
            throw new InvalidOperationException($"Failed to create payment link: {ex.Message}", ex);
        }
    }

    public bool VerifyWebhookSignature(string data, string signature)
    {
        try
        {
            var checksumKey = _config["PayOS:ChecksumKey"] ?? "";
            if (string.IsNullOrEmpty(checksumKey))
            {
                _logger.LogError("PayOS ChecksumKey is missing in configuration.");
                return false;
            }

            // PayOS uses HMAC SHA256 for signature verification
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(checksumKey));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    public async Task<PayOsWebhookResult> HandleWebhookAsync(PayOsWebhookDto webhookData)
    {
        try
        {
            if (webhookData == null)
            {
                _logger.LogWarning("PayOS webhook data is null");
                return new PayOsWebhookResult
                {
                    Success = false,
                    Message = "Webhook data is null"
                };
            }

            // Convert webhook DTO to PayOS SDK Webhook model (like in official demo)
            var webhook = new PayOS.Models.Webhooks.Webhook
            {
                Code = webhookData.Code,
                Data = webhookData.Data != null ? new PayOS.Models.Webhooks.WebhookData
                {
                    OrderCode = webhookData.Data.OrderCode,
                    Amount = webhookData.Data.Amount,
                    Description = webhookData.Data.Description,
                    AccountNumber = webhookData.Data.AccountNumber,
                    Reference = webhookData.Data.Reference,
                    TransactionDateTime = webhookData.Data.TransactionDateTime,
                    Currency = webhookData.Data.Currency,
                    PaymentLinkId = webhookData.Data.PaymentLinkId,
                    VirtualAccountName = webhookData.Data.VirtualAccountName,
                    VirtualAccountNumber = webhookData.Data.VirtualAccountNumber,
                    CounterAccountBankId = webhookData.Data.CounterAccountBankId?.ToString(),
                    CounterAccountBankName = webhookData.Data.CounterAccountBankName?.ToString(),
                    CounterAccountName = webhookData.Data.CounterAccountName?.ToString(),
                    CounterAccountNumber = webhookData.Data.CounterAccountNumber?.ToString()
                } : null
            };

            // Use PayOS SDK to verify webhook (like in official demo)
            var webhookDataVerified = await _payOSClient.Webhooks.VerifyAsync(webhook);

            var orderCode = webhookDataVerified.OrderCode; // Keep as long
            // Status is not in WebhookData, get from original webhook data
            var status = webhookData?.Data?.Status ?? "";

            _logger.LogInformation("PayOS webhook verified for order {OrderCode} with status {Status}", orderCode, status);

            return new PayOsWebhookResult
            {
                Success = true,
                OrderCode = (int)orderCode, // Cast to int for DTO
                Status = status,
                Message = "Webhook processed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PayOS webhook");
            return new PayOsWebhookResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Lấy thông tin payment từ PayOS bằng PaymentLinkId
    /// </summary>
    public async Task<PayOsPaymentLinkDto?> GetPaymentInformationAsync(string paymentLinkId)
    {
        try
        {
            _logger.LogInformation("Getting payment information for PaymentLinkId {PaymentLinkId} from PayOS", paymentLinkId);

            if (string.IsNullOrEmpty(paymentLinkId))
            {
                _logger.LogWarning("PaymentLinkId is null or empty");
                return null;
            }

            // PayOS SDK có thể lấy payment info bằng PaymentLinkId
            // PaymentRequests.GetAsync nhận PaymentLinkId (string)
            var paymentInfo = await _payOSClient.PaymentRequests.GetAsync(paymentLinkId);

            if (paymentInfo == null)
            {
                _logger.LogWarning("Payment information not found for PaymentLinkId {PaymentLinkId}", paymentLinkId);
                return null;
            }

            // Convert Status enum to string
            var statusString = paymentInfo.Status.ToString();

            _logger.LogInformation("Payment information retrieved for PaymentLinkId {PaymentLinkId}: Status={Status}, OrderCode={OrderCode}",
                paymentLinkId, statusString, paymentInfo.OrderCode);

            // PaymentLink từ GetAsync chỉ cần Status, không cần CheckoutUrl và QrCode
            // Vì chúng ta chỉ cần status để sync payment
            return new PayOsPaymentLinkDto
            {
                PaymentLinkId = paymentLinkId,
                PaymentLink = "", // Not needed for sync
                QrCode = null, // Not needed for sync
                Status = statusString // Convert enum to string: PAID, PENDING, CANCELLED
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment information for PaymentLinkId {PaymentLinkId}: {Message}", paymentLinkId, ex.Message);
            return null;
        }
    }
}

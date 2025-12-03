using BitNow_Backend.BLL.Payment;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPayOsService _payOsService;
    private readonly IOrderService _orderService;
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _config;

    public PaymentController(
        IPayOsService payOsService,
        IOrderService orderService,
        ILogger<PaymentController> logger,
        IConfiguration config)
    {
        _payOsService = payOsService;
        _orderService = orderService;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Tạo payment link cho order (chỉ cần orderId, số tiền lấy từ order)
    /// </summary>
    [HttpPost("create-link")]
    public async Task<ActionResult<PayOsPaymentLinkDto>> CreatePaymentLink([FromBody] CreatePaymentLinkRequestDto request)
    {
        try
        {
            // Get order by ID
            var order = await _orderService.GetOrderByIdAsync(request.OrderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            if (order.OrderStatus != "awaiting_payment")
            {
                return BadRequest(new { message = "Order is not in awaiting_payment status" });
            }

            // Get default URLs from appsettings.json or use defaults
            // PayOS requires full URLs (http:// or https://)
            var defaultReturnUrl = _config["PayOS:ReturnUrl"] ?? $"https://{Request.Host}/payment/success";
            var defaultCancelUrl = _config["PayOS:CancelUrl"] ?? $"https://{Request.Host}/payment/cancel";

            // Append orderId to URLs if not already present
            var returnUrl = defaultReturnUrl.Contains("orderId=") ? defaultReturnUrl : defaultReturnUrl + (defaultReturnUrl.Contains("?") ? "&" : "?") + $"orderId={order.Id}";
            var cancelUrl = defaultCancelUrl.Contains("orderId=") ? defaultCancelUrl : defaultCancelUrl + (defaultCancelUrl.Contains("?") ? "&" : "?") + $"orderId={order.Id}";
            
            // Ensure URLs are absolute (PayOS requirement)
            if (!returnUrl.StartsWith("http://") && !returnUrl.StartsWith("https://"))
            {
                returnUrl = $"https://{returnUrl}";
            }
            if (!cancelUrl.StartsWith("http://") && !cancelUrl.StartsWith("https://"))
            {
                cancelUrl = $"https://{cancelUrl}";
            }

            // Description must be max 25 characters for PayOS
            var description = $"Don hang #{order.Id}";

            _logger.LogInformation("Creating payment link for order {OrderId}, Amount={Amount}, ReturnUrl={ReturnUrl}, CancelUrl={CancelUrl}", 
                order.Id, order.FinalPrice, returnUrl, cancelUrl);

            try
            {
                // Create payment link - amount is taken from order.FinalPrice
                var paymentLink = await _payOsService.CreatePaymentLinkAsync(
                    order.Id,
                    order.FinalPrice, // Amount from order
                    description,
                    returnUrl,
                    cancelUrl
                );

                _logger.LogInformation("Payment link created successfully for order {OrderId}", order.Id);
                return Ok(paymentLink);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PayOS error creating payment link for order {OrderId}: {Message}", order.Id, ex.Message);
                return StatusCode(500, new { message = ex.Message, error = "PayOS API error" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment link for order {OrderId}", request.OrderId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Webhook endpoint để nhận thông báo từ PayOS khi payment thành công/thất bại
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook([FromBody] PayOsWebhookDto webhook)
    {
        try
        {
            _logger.LogInformation("PayOS webhook received");

            var result = await _payOsService.HandleWebhookAsync(webhook);

            if (!result.Success)
            {
                _logger.LogWarning("Webhook processing failed: {Message}", result.Message);
                return BadRequest(new { message = result.Message });
            }

            // Update order and payment status based on webhook result
            // Note: PayOS returns orderCode (Unix timestamp), we need to find order by other means
            // For now, we'll use the orderCode from webhook and try to find order by payment link
            if (result.OrderCode.HasValue)
            {
                _logger.LogInformation("PayOS webhook orderCode: {OrderCode}", result.OrderCode.Value);
                
                // Try to find order by payment link ID from webhook
                // Note: This is a simplified approach - in production, you might want to store orderCode mapping
                var orderId = 0; // Will be determined from webhook data if available
                
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order != null)
                {
                    if (result.Status == "PAID")
                    {
                        await _orderService.UpdateOrderStatusAsync(order.Id, "awaiting_shipment");
                        await UpdatePaymentStatusAsync(order.Id, "paid_held", webhook.Data);
                    }
                    else if (result.Status == "CANCELLED")
                    {
                        await UpdatePaymentStatusAsync(order.Id, "pending", webhook.Data);
                    }
                }
            }

            return Ok(new { message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS webhook");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy order theo auction ID (để frontend có thể tạo payment link)
    /// </summary>
    [HttpGet("auction/{auctionId}/order")]
    public async Task<ActionResult<OrderDto>> GetOrderByAuctionId(int auctionId)
    {
        try
        {
            var order = await _orderService.GetOrderByAuctionIdAsync(auctionId);
            
            if (order == null)
            {
                return NotFound(new { message = "Order not found for this auction" });
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order for auction {AuctionId}", auctionId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    private async Task UpdatePaymentStatusAsync(int orderId, string status, PayOsWebhookData? webhookData)
    {
        try
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BitNow_Backend.DAL.BidNowDbContext>();

            var payment = await dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment == null)
            {
                payment = new BitNow_Backend.DAL.Models.Payment
                {
                    OrderId = orderId,
                    Amount = webhookData?.Amount ?? 0,
                    PaymentStatus = status,
                    PaymentMethod = "payos",
                    PaymentProvider = "PayOS",
                    TransactionId = webhookData?.PaymentLinkId,
                    CreatedAt = DateTime.Now
                };

                if (status == "paid_held" && webhookData != null)
                {
                    payment.PaidAt = DateTime.Now;
                    if (!string.IsNullOrEmpty(webhookData.TransactionDateTime))
                    {
                        if (DateTime.TryParse(webhookData.TransactionDateTime, out var paidDate))
                        {
                            payment.PaidAt = paidDate;
                        }
                    }
                }

                dbContext.Payments.Add(payment);
            }
            else
            {
                payment.PaymentStatus = status;
                payment.UpdatedAt = DateTime.Now;

                if (status == "paid_held" && payment.PaidAt == null)
                {
                    payment.PaidAt = DateTime.Now;
                    if (webhookData != null && !string.IsNullOrEmpty(webhookData.TransactionDateTime))
                    {
                        if (DateTime.TryParse(webhookData.TransactionDateTime, out var paidDate))
                        {
                            payment.PaidAt = paidDate;
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status for order {OrderId}", orderId);
        }
    }
}


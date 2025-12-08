using BitNow_Backend.BLL.Payment;
using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPayOsService _payOsService;
    private readonly IOrderService _orderService;
    private readonly INotificationService? _notificationService;
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _config;

    public PaymentController(
        IPayOsService payOsService,
        IOrderService orderService,
        ILogger<PaymentController> logger,
        IConfiguration config,
        IServiceProvider serviceProvider)
    {
        _payOsService = payOsService;
        _orderService = orderService;
        _notificationService = serviceProvider.GetService<INotificationService>();
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

                // Save payment link ID to order for webhook lookup
                if (!string.IsNullOrEmpty(paymentLink.PaymentLinkId))
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<BitNow_Backend.DAL.BidNowDbContext>();
                    
                    var dbOrder = await dbContext.Orders.FindAsync(order.Id);
                    if (dbOrder != null)
                    {
                        // Store payment link ID in a way we can look it up later
                        // We'll use Payment table to store this
                        var payment = await dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
                        if (payment == null)
                        {
                            payment = new BitNow_Backend.DAL.Models.Payment
                            {
                                OrderId = order.Id,
                                Amount = order.FinalPrice,
                                PaymentStatus = "pending",
                                PaymentMethod = "payos",
                                PaymentProvider = "PayOS",
                                TransactionId = paymentLink.PaymentLinkId,
                                CreatedAt = DateTime.Now
                            };
                            dbContext.Payments.Add(payment);
                        }
                        else
                        {
                            payment.TransactionId = paymentLink.PaymentLinkId;
                            payment.UpdatedAt = DateTime.Now;
                        }
                        await dbContext.SaveChangesAsync();
                    }
                }

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
            // PayOS returns orderCode (Unix timestamp) and PaymentLinkId in webhook
            // We can find order by PaymentLinkId stored in Payment table
            if (result.OrderCode.HasValue && webhook.Data != null && !string.IsNullOrEmpty(webhook.Data.PaymentLinkId))
            {
                _logger.LogInformation("PayOS webhook orderCode: {OrderCode}, PaymentLinkId: {PaymentLinkId}, Status: {Status}", 
                    result.OrderCode.Value, webhook.Data.PaymentLinkId, result.Status);
                
                // Find order by payment link ID
                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BitNow_Backend.DAL.BidNowDbContext>();
                
                var payment = await dbContext.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == webhook.Data.PaymentLinkId);
                
                if (payment != null)
                {
                    _logger.LogInformation("Found payment record: OrderId={OrderId}, CurrentStatus={CurrentStatus}", 
                        payment.OrderId, payment.PaymentStatus);
                    
                    var order = await _orderService.GetOrderByIdAsync(payment.OrderId);
                    if (order != null)
                    {
                        _logger.LogInformation("Processing webhook for order {OrderId}, Status: {Status}", 
                            order.Id, result.Status);
                        
                        if (result.Status == "PAID")
                        {
                            _logger.LogInformation("Updating order {OrderId} to awaiting_shipment and payment to paid_held", order.Id);
                            await _orderService.UpdateOrderStatusAsync(order.Id, "awaiting_shipment");
                            await UpdatePaymentStatusAsync(order.Id, "paid_held", webhook.Data);
                            
                            // Notify seller that payment was received
                            if (_notificationService != null && order.SellerId > 0)
                            {
                                try
                                {
                                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                                    {
                                        UserId = order.SellerId,
                                        Message = $"Đơn hàng #{order.Id} đã được thanh toán thành công. Vui lòng chuẩn bị và gửi hàng.",
                                        Type = "order_payment_received",
                                        Link = $"/seller?tab=orders"
                                    });
                                    _logger.LogInformation("Notification sent to seller {SellerId} for order {OrderId}", order.SellerId, order.Id);
                                }
                                catch (Exception notifEx)
                                {
                                    _logger.LogError(notifEx, "Failed to send notification to seller for order {OrderId}", order.Id);
                                }
                            }
                            
                            _logger.LogInformation("Successfully updated order {OrderId} and payment status", order.Id);
                        }
                        else if (result.Status == "CANCELLED")
                        {
                            _logger.LogInformation("Updating payment for order {OrderId} to pending (cancelled)", order.Id);
                            await UpdatePaymentStatusAsync(order.Id, "pending", webhook.Data);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Order {OrderId} not found for payment", payment.OrderId);
                    }
                }
                else
                {
                    _logger.LogWarning("Payment not found for PaymentLinkId: {PaymentLinkId}. Searching all payments...", webhook.Data.PaymentLinkId);
                    
                    // Try to find by orderCode if PaymentLinkId doesn't match
                    // OrderCode is Unix timestamp, we can try to find order by it
                    // But first, let's log all payments to debug
                    var allPayments = await dbContext.Payments
                        .Select(p => new { p.Id, p.OrderId, p.TransactionId, p.PaymentStatus })
                        .ToListAsync();
                    _logger.LogInformation("All payments in database: {Payments}", 
                        string.Join(", ", allPayments.Select(p => $"Id={p.Id}, OrderId={p.OrderId}, TransactionId={p.TransactionId}, Status={p.PaymentStatus}")));
                }
            }
            else
            {
                _logger.LogWarning("Webhook data missing required fields. OrderCode: {OrderCode}, PaymentLinkId: {PaymentLinkId}", 
                    result.OrderCode, webhook.Data?.PaymentLinkId);
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
    /// Đồng bộ lại trạng thái thanh toán cho order từ PayOS API (để fix trường hợp webhook chưa được gọi)
    /// </summary>
    [HttpPost("order/{orderId}/sync-payment")]
    public async Task<ActionResult<OrderDto>> SyncPaymentStatus(int orderId)
    {
        try
        {
            _logger.LogInformation("Syncing payment status for order {OrderId} from PayOS API", orderId);

            using var scope = HttpContext.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BitNow_Backend.DAL.BidNowDbContext>();

            var order = await dbContext.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            var payment = order.Payment;
            if (payment == null || string.IsNullOrEmpty(payment.TransactionId))
            {
                _logger.LogWarning("No payment record or TransactionId found for order {OrderId}", orderId);
                var orderDto = await _orderService.GetOrderByIdAsync(orderId);
                return Ok(orderDto);
            }

            _logger.LogInformation("Current payment status for order {OrderId}: {Status}, PaymentLinkId: {PaymentLinkId}", 
                orderId, payment.PaymentStatus, payment.TransactionId);
            
            // Gọi PayOS API để lấy payment status thực tế
            var payOsPaymentInfo = await _payOsService.GetPaymentInformationAsync(payment.TransactionId);

            if (payOsPaymentInfo == null)
            {
                _logger.LogWarning("Could not retrieve payment information from PayOS for order {OrderId}, PaymentLinkId {PaymentLinkId}",
                    orderId, payment.TransactionId);
                var orderDto = await _orderService.GetOrderByIdAsync(orderId);
                return Ok(orderDto);
            }

            var payOsStatus = payOsPaymentInfo.Status?.Trim().ToUpperInvariant() ?? "";
            _logger.LogInformation("PayOS status for order {OrderId}: {Status} (current DB status: {CurrentStatus})", 
                orderId, payOsStatus, payment.PaymentStatus);

            // STRICT CHECK: Chỉ cập nhật khi PayOS status chính xác là "PAID" (case-insensitive)
            // Không cho phép cập nhật nếu status là CANCELLED, PENDING, hoặc bất kỳ giá trị nào khác
            if (payOsStatus == "PAID" && payment.PaymentStatus != "paid_held" && order.OrderStatus == "awaiting_payment")
            {
                _logger.LogInformation("Updating order {OrderId} from PayOS sync: Status=PAID, updating to awaiting_shipment and paid_held", orderId);
                
                await _orderService.UpdateOrderStatusAsync(order.Id, "awaiting_shipment");
                await UpdatePaymentStatusAsync(order.Id, "paid_held", null);
                
                // Notify seller
                if (_notificationService != null && order.SellerId > 0)
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = order.SellerId,
                            Message = $"Đơn hàng #{order.Id} đã được thanh toán thành công (đồng bộ từ PayOS). Vui lòng chuẩn bị và gửi hàng.",
                            Type = "order_payment_received",
                            Link = $"/seller?tab=orders"
                        });
                        _logger.LogInformation("Notification sent to seller {SellerId} for synced payment order {OrderId}", order.SellerId, order.Id);
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogError(notifEx, "Failed to send notification to seller for synced payment order {OrderId}", order.Id);
                    }
                }
                
                _logger.LogInformation("Order {OrderId} payment status synced to PAID from PayOS", orderId);
            }
            else if (payOsStatus == "CANCELLED" && payment.PaymentStatus != "pending")
            {
                _logger.LogInformation("Updating payment for order {OrderId} to pending (cancelled from PayOS)", orderId);
                await UpdatePaymentStatusAsync(order.Id, "pending", null);
            }
            else if (payOsStatus == "PENDING" || payOsStatus == "")
            {
                // PayOS status vẫn là PENDING hoặc empty - không cập nhật gì cả
                _logger.LogInformation("Order {OrderId} payment status on PayOS is {Status}, keeping current DB status: {DbStatus}", 
                    orderId, payOsStatus, payment.PaymentStatus);
            }
            else
            {
                // Status khác (không phải PAID, CANCELLED, PENDING) - log warning và không cập nhật
                _logger.LogWarning("Order {OrderId} has unexpected PayOS status: {Status}, not updating. Current DB status: {DbStatus}", 
                    orderId, payOsStatus, payment.PaymentStatus);
            }

            var updatedOrder = await _orderService.GetOrderByIdAsync(orderId);
            return Ok(updatedOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing payment status for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Manually update payment status (for cases where webhook wasn't called but payment was completed)
    /// </summary>
    [HttpPost("order/{orderId}/mark-paid")]
    public async Task<ActionResult<OrderDto>> MarkOrderAsPaid(int orderId)
    {
        try
        {
            _logger.LogInformation("Manually marking order {OrderId} as paid", orderId);

            using var scope = HttpContext.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BitNow_Backend.DAL.BidNowDbContext>();

            var order = await dbContext.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            // Update payment status to paid_held
            var payment = order.Payment;
            if (payment == null)
            {
                // Create payment record if it doesn't exist
                payment = new BitNow_Backend.DAL.Models.Payment
                {
                    OrderId = orderId,
                    Amount = order.FinalPrice,
                    PaymentStatus = "paid_held",
                    PaymentMethod = "payos",
                    PaymentProvider = "PayOS",
                    PaidAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                };
                dbContext.Payments.Add(payment);
            }
            else
            {
                payment.PaymentStatus = "paid_held";
                payment.PaidAt = payment.PaidAt ?? DateTime.Now;
                payment.UpdatedAt = DateTime.Now;
            }

            // Update order status to awaiting_shipment
            if (order.OrderStatus == "awaiting_payment")
            {
                order.OrderStatus = "awaiting_shipment";
                order.UpdatedAt = DateTime.Now;
            }

            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} marked as paid successfully", orderId);

            var orderDto = await _orderService.GetOrderByIdAsync(orderId);
            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking order {OrderId} as paid", orderId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy order theo auction ID (để frontend có thể tạo payment link)
    /// Nếu order chưa tồn tại và auction đã completed với winner, tự động tạo order
    /// </summary>
    [HttpGet("auction/{auctionId}/order")]
    public async Task<ActionResult<OrderDto>> GetOrderByAuctionId(int auctionId)
    {
        try
        {
            _logger.LogInformation("Getting order for auction {AuctionId}", auctionId);

            var order = await _orderService.GetOrderByAuctionIdAsync(auctionId);

            // If order doesn't exist, check if auction is completed and create order
            if (order == null)
            {
                _logger.LogInformation("Order not found for auction {AuctionId}, checking auction status...", auctionId);

                using (var scope = HttpContext.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<BitNow_Backend.DAL.BidNowDbContext>();

                    var auction = await dbContext.Auctions
                        .Include(a => a.Item)
                        .FirstOrDefaultAsync(a => a.Id == auctionId);

                    if (auction == null)
                    {
                        _logger.LogWarning("Auction {AuctionId} not found", auctionId);
                        return NotFound(new { message = "Auction not found" });
                    }

                    _logger.LogInformation("Auction {AuctionId} found: Status={Status}, WinnerId={WinnerId}, CurrentBid={CurrentBid}",
                        auctionId, auction.Status, auction.WinnerId, auction.CurrentBid);

                    // Only create order if auction is completed and has a winner
                    // Use case-insensitive comparison
                    var statusLower = auction.Status?.ToLower() ?? "";
                    if (statusLower == "completed" && auction.WinnerId.HasValue && auction.CurrentBid.HasValue)
                    {
                        _logger.LogInformation("Auto-creating order for completed auction {AuctionId} with winner {WinnerId}, finalPrice={FinalPrice}",
                            auctionId, auction.WinnerId.Value, auction.CurrentBid.Value);

                        try
                        {
                            order = await _orderService.CreateOrderForWinnerAsync(
                                auctionId,
                                auction.WinnerId.Value,
                                auction.CurrentBid.Value
                            );

                            _logger.LogInformation("Order {OrderId} created successfully for auction {AuctionId}", order.Id, auctionId);
                            return Ok(order);
                        }
                        catch (Exception createEx)
                        {
                            _logger.LogError(createEx, "Error auto-creating order for auction {AuctionId}: {Message}",
                                auctionId, createEx.Message);
                            return StatusCode(500, new { message = "Error creating order", error = createEx.Message });
                        }
                    }

                    _logger.LogWarning("Cannot create order for auction {AuctionId}: Status={Status}, HasWinner={HasWinner}, HasCurrentBid={HasCurrentBid}",
                        auctionId, auction.Status, auction.WinnerId.HasValue, auction.CurrentBid.HasValue);

                    return NotFound(new
                    {
                        message = "Order not found for this auction. Auction may not be completed yet.",
                        auctionStatus = auction.Status,
                        hasWinner = auction.WinnerId.HasValue,
                        hasFinalBid = auction.CurrentBid.HasValue,
                        winnerId = auction.WinnerId,
                        currentBid = auction.CurrentBid
                    });
                }
            }

            _logger.LogInformation("Order {OrderId} found for auction {AuctionId}", order.Id, auctionId);
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order for auction {AuctionId}: {Message}", auctionId, ex.Message);
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
                    Amount = webhookData?.Amount > 0 ? (decimal)webhookData.Amount : 0,
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

    /// <summary>
    /// Lấy orders của buyer
    /// </summary>
    [HttpGet("buyer/{buyerId}/orders")]
    public async Task<ActionResult<List<OrderDto>>> GetBuyerOrders(int buyerId)
    {
        try
        {
            var orders = await _orderService.GetOrdersByBuyerIdAsync(buyerId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for buyer {BuyerId}", buyerId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy orders của seller
    /// </summary>
    [HttpGet("seller/{sellerId}/orders")]
    public async Task<ActionResult<List<OrderDto>>> GetSellerOrders(int sellerId)
    {
        try
        {
            var orders = await _orderService.GetOrdersBySellerIdAsync(sellerId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for seller {SellerId}", sellerId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin vận chuyển cho order (seller)
    /// </summary>
    [HttpPost("order/{orderId}/update-shipping")]
    public async Task<ActionResult<OrderDto>> UpdateShippingInfo(int orderId, [FromBody] UpdateShippingInfoDto dto)
    {
        try
        {
            _logger.LogInformation("Updating shipping info for order {OrderId}", orderId);

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.TrackingNumber))
            {
                return BadRequest(new { message = "Tracking number is required" });
            }

            var success = await _orderService.UpdateShippingInfoAsync(
                orderId,
                dto.TrackingNumber,
                dto.ShippingCompany,
                dto.ShippingAddress
            );

            if (!success)
            {
                return BadRequest(new { message = "Failed to update shipping info. Order may not be in awaiting_shipment status." });
            }

            // Notify buyer that order has been shipped
            if (_notificationService != null && order.BuyerId > 0)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = order.BuyerId,
                        Message = $"Đơn hàng #{order.Id} đã được gửi. Mã vận đơn: {dto.TrackingNumber}",
                        Type = "order_shipped",
                        Link = $"/buyer?tab=orders"
                    });
                    _logger.LogInformation("Notification sent to buyer {BuyerId} for shipped order {OrderId}", order.BuyerId, order.Id);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send notification to buyer for order {OrderId}", orderId);
                }
            }

            var updatedOrder = await _orderService.GetOrderByIdAsync(orderId);
            return Ok(updatedOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipping info for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Buyer xác nhận đã nhận hàng
    /// </summary>
    [HttpPost("order/{orderId}/confirm-received")]
    public async Task<ActionResult<OrderDto>> ConfirmOrderReceived(int orderId)
    {
        try
        {
            _logger.LogInformation("Buyer confirming receipt for order {OrderId}", orderId);

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            // Only allow confirmation if order is shipped
            if (order.OrderStatus != "shipped")
            {
                return BadRequest(new { message = "Order must be shipped before confirmation" });
            }

            var success = await _orderService.UpdateOrderStatusAsync(orderId, "completed");
            if (!success)
            {
                return BadRequest(new { message = "Failed to update order status" });
            }

            // Release payment to seller
            using var scope = HttpContext.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BitNow_Backend.DAL.BidNowDbContext>();
            
            var payment = await dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment != null && payment.PaymentStatus == "paid_held")
            {
                payment.PaymentStatus = "released_to_seller";
                payment.ReleasedAt = DateTime.Now;
                payment.UpdatedAt = DateTime.Now;
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Payment released to seller for order {OrderId}", orderId);
            }

            // Notify seller
            if (_notificationService != null && order.SellerId > 0)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = order.SellerId,
                        Message = $"Đơn hàng #{order.Id} đã được buyer xác nhận nhận hàng. Tiền đã được giải phóng.",
                        Type = "order_completed",
                        Link = $"/seller?tab=orders"
                    });
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send notification to seller for completed order {OrderId}", orderId);
                }
            }

            var updatedOrder = await _orderService.GetOrderByIdAsync(orderId);
            return Ok(updatedOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order receipt for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Buyer báo cáo sự cố với order
    /// </summary>
    [HttpPost("order/{orderId}/report-issue")]
    public async Task<ActionResult<OrderDto>> ReportOrderIssue(int orderId, [FromBody] ReportIssueDto dto)
    {
        try
        {
            _logger.LogInformation("Buyer reporting issue for order {OrderId}", orderId);

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            // Get buyer ID from header (custom authentication)
            var userIdHeader = Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdHeader) || !int.TryParse(userIdHeader, out var buyerId))
            {
                // Fallback: try to get from User claims if available
                var buyerIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(buyerIdClaim) || !int.TryParse(buyerIdClaim, out buyerId))
                {
                    return Unauthorized(new { message = "User not authenticated. Please provide X-User-Id header." });
                }
            }

            if (order.BuyerId != buyerId)
            {
                return Forbid();
            }

            // Create dispute
            var disputeService = HttpContext.RequestServices.GetRequiredService<BitNow_Backend.BLL.IServices.IDisputeService>();
            try
            {
                var createDisputeDto = new CreateDisputeDto
                {
                    OrderId = orderId,
                    Reason = dto.IssueDescription,
                    Description = dto.IssueDescription
                };
                await disputeService.CreateDisputeAsync(createDisputeDto, buyerId);
                _logger.LogInformation("Dispute created successfully for order {OrderId}", orderId);
            }
            catch (InvalidOperationException ex)
            {
                // Dispute already exists - this is OK, continue
                _logger.LogInformation("Dispute already exists for order {OrderId}: {Message}", orderId, ex.Message);
            }
            catch (Exception disputeEx)
            {
                _logger.LogError(disputeEx, "Failed to create dispute for order {OrderId}", orderId);
                // Continue even if dispute creation fails - order status will still be updated
            }

            // Update order status to dispute
            var success = await _orderService.UpdateOrderStatusAsync(orderId, "dispute");
            if (!success)
            {
                return BadRequest(new { message = "Failed to update order status" });
            }

            // Notify seller
            if (_notificationService != null && order.SellerId > 0)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = order.SellerId,
                        Message = $"Đơn hàng #{order.Id} có sự cố: {dto.IssueDescription}",
                        Type = "order_dispute",
                        Link = $"/seller?tab=orders"
                    });
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send notification to seller for disputed order {OrderId}", orderId);
                }
            }

            var updatedOrder = await _orderService.GetOrderByIdAsync(orderId);
            return Ok(updatedOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting issue for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}

public class UpdateShippingInfoDto
{
    public string TrackingNumber { get; set; } = null!;
    public string? ShippingCompany { get; set; }
    public string? ShippingAddress { get; set; }
}

public class ReportIssueDto
{
    public string IssueDescription { get; set; } = null!;
}


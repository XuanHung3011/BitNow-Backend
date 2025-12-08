namespace BitNow_Backend.DAL.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public int AuctionId { get; set; }
    public int BuyerId { get; set; }
    public int SellerId { get; set; }
    public decimal FinalPrice { get; set; }
    public string OrderStatus { get; set; } = null!; // awaiting_payment, awaiting_shipment, shipped, dispute, completed, cancelled
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ShippingCompany { get; set; }
    public DateTime? ShippedAt { get; set; }
    public string? ShippingAddress { get; set; }
    public PaymentDto? Payment { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = null!; // pending, paid_held, hold_dispute, refunded_to_buyer, released_to_seller
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentProvider { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Notes { get; set; }
}

public class PayOsPaymentLinkDto
{
    public int OrderId { get; set; }
    public string PaymentLink { get; set; } = null!;
    public string? PaymentLinkId { get; set; }
    public string? QrCode { get; set; }
    public string? Status { get; set; } // PAID, PENDING, CANCELLED
}

public class CreatePaymentLinkRequestDto
{
    public int OrderId { get; set; }
    // ReturnUrl and CancelUrl are optional - will use defaults from appsettings.json if not provided
}

public class PayOsWebhookDto
{
    public string Code { get; set; } = null!;
    public string? Desc { get; set; }
    public PayOsWebhookData? Data { get; set; }
}

public class PayOsWebhookData
{
    public long OrderCode { get; set; } // Changed to long to match SDK
    public long Amount { get; set; } // Changed to long to match SDK
    public string? Description { get; set; }
    public string? AccountNumber { get; set; }
    public string? Reference { get; set; }
    public string? TransactionDateTime { get; set; }
    public string? Currency { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? Code { get; set; }
    public string? Desc { get; set; }
    public string? CounterAccountBankId { get; set; }
    public string? CounterAccountBankName { get; set; }
    public string? CounterAccountName { get; set; }
    public string? CounterAccountNumber { get; set; }
    public string? VirtualAccountName { get; set; }
    public string? VirtualAccountNumber { get; set; }
    public string? Status { get; set; } // PAID, CANCELLED
}

public class PayOsWebhookResult
{
    public bool Success { get; set; }
    public int? OrderCode { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
}


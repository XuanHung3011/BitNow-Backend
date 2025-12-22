using System;

namespace BitNow_Backend.DAL.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentStatus { get; set; } = null!; // pending, paid_held, hold_dispute, refunded_to_buyer, released_to_seller

    public string? PaymentMethod { get; set; } // credit_card, bank_transfer, e_wallet, etc.

    public string? TransactionId { get; set; }

    public string? PaymentProvider { get; set; } // stripe, paypal, vnpay, etc.

    public DateTime? PaidAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public DateTime? RefundedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Notes { get; set; }

    // Navigation property
    public virtual Order Order { get; set; } = null!;
}




using System;

namespace BitNow_Backend.DAL.Models;

public partial class Dispute
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int BuyerId { get; set; }

    public int SellerId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!; // pending, in_review, buyer_won, seller_won, resolved, closed

    public string? Resolution { get; set; } // refunded_to_buyer, released_to_seller

    public int? ResolvedBy { get; set; } // Admin/Support user ID

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? AdminNotes { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;

    public virtual User Buyer { get; set; } = null!;

    public virtual User Seller { get; set; } = null!;

    public virtual User? Resolver { get; set; }
}




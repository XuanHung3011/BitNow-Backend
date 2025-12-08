namespace BitNow_Backend.DAL.DTOs;

public class DisputeDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int BuyerId { get; set; }
    public string BuyerName { get; set; } = null!;
    public int SellerId { get; set; }
    public string SellerName { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = null!; // pending, in_review, buyer_won, seller_won, resolved, closed
    public string? Resolution { get; set; } // refunded_to_buyer, released_to_seller
    public int? ResolvedBy { get; set; }
    public string? ResolverName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? AdminNotes { get; set; }
    public OrderDto? Order { get; set; }
    public string? AuctionTitle { get; set; }
    public decimal? OrderAmount { get; set; }
}

public class CreateDisputeDto
{
    public int OrderId { get; set; }
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
}

public class UpdateDisputeStatusDto
{
    public string Status { get; set; } = null!; // in_review, buyer_won, seller_won, resolved, closed
    public string? AdminNotes { get; set; }
}

public class ResolveDisputeDto
{
    public string Winner { get; set; } = null!; // "buyer" or "seller"
    public string? AdminNotes { get; set; }
}


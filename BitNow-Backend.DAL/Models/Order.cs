using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class Order
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

    // Navigation properties
    public virtual Auction Auction { get; set; } = null!;

    public virtual User Buyer { get; set; } = null!;

    public virtual User Seller { get; set; } = null!;

    public virtual Payment? Payment { get; set; }

    public virtual Dispute? Dispute { get; set; }
}


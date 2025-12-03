using System;

namespace BitNow_Backend.DAL.Models;

public partial class UserAuctionView
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int AuctionId { get; set; }

    public DateTime ViewedAt { get; set; }

    public virtual User? User { get; set; }

    public virtual Auction? Auction { get; set; }
}


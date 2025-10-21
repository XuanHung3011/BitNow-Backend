using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class Bid
{
    public int Id { get; set; }

    public int AuctionId { get; set; }

    public int BidderId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? BidTime { get; set; }

    public bool? IsAutoBid { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual User Bidder { get; set; } = null!;
}



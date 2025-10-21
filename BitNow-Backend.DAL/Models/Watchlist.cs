using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class Watchlist
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int AuctionId { get; set; }

    public DateTime? AddedAt { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}



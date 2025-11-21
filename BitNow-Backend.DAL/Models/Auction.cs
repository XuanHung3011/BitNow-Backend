using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class Auction
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    public int SellerId { get; set; }

    public decimal StartingBid { get; set; }

    public decimal? CurrentBid { get; set; }

    public decimal? BuyNowPrice { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string Status { get; set; } = null!;

    public int? BidCount { get; set; }

    public int? WinnerId { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? PausedAt { get; set; }


    public virtual ICollection<AutoBid> AutoBids { get; set; } = new List<AutoBid>();

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual Item Item { get; set; } = null!;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual User Seller { get; set; } = null!;

    public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();

    public virtual User? Winner { get; set; }
}



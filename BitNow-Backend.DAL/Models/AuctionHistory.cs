using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class AuctionHistory
{
    public int Id { get; set; }

    public int AuctionId { get; set; }

    public int ItemId { get; set; }

    public string Title { get; set; } = null!;

    public int CategoryId { get; set; }

    public int SellerId { get; set; }

    public int? WinnerId { get; set; }

    public decimal StartingBid { get; set; }

    public decimal? FinalBid { get; set; }

    public int? TotalBids { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public DateTime? CompletedAt { get; set; }
}



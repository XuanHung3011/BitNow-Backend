using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class Rating
{
    public int Id { get; set; }

    public int AuctionId { get; set; }

    public int RaterId { get; set; }

    public int RatedId { get; set; }

    public int Rating1 { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual User Rated { get; set; } = null!;

    public virtual User Rater { get; set; } = null!;
}



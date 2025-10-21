using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class Item
{
    public int Id { get; set; }

    public int SellerId { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Images { get; set; }

    public string? Condition { get; set; }

    public string? Location { get; set; }

    public decimal BasePrice { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Auction> Auctions { get; set; } = new List<Auction>();

    public virtual Category Category { get; set; } = null!;

    public virtual User Seller { get; set; } = null!;
}



using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class FavoriteSeller
{
    public int Id { get; set; }

    public int BuyerId { get; set; }

    public int SellerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Buyer { get; set; } = null!;

    public virtual User Seller { get; set; } = null!;
}



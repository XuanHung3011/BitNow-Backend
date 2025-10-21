using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public decimal? ReputationScore { get; set; }

    public int? TotalRatings { get; set; }

    public int? TotalSales { get; set; }

    public int? TotalPurchases { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Auction> AuctionSellers { get; set; } = new List<Auction>();

    public virtual ICollection<Auction> AuctionWinners { get; set; } = new List<Auction>();

    public virtual ICollection<AutoBid> AutoBids { get; set; } = new List<AutoBid>();

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual ICollection<ContactMessage> ContactMessages { get; set; } = new List<ContactMessage>();

    public virtual ICollection<FavoriteSeller> FavoriteSellerBuyers { get; set; } = new List<FavoriteSeller>();

    public virtual ICollection<FavoriteSeller> FavoriteSellerSellers { get; set; } = new List<FavoriteSeller>();

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<Message> MessageReceivers { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Rating> RatingRateds { get; set; } = new List<Rating>();

    public virtual ICollection<Rating> RatingRaters { get; set; } = new List<Rating>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();
}



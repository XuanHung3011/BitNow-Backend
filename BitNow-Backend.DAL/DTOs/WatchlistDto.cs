using System;

namespace BitNow_Backend.DAL.DTOs
{
	public class AddToWatchlistRequest
	{
		public int UserId { get; set; }
		public int AuctionId { get; set; }
	}

	public class RemoveFromWatchlistRequest
	{
		public int UserId { get; set; }
		public int AuctionId { get; set; }
	}

	public class WatchlistItemDto
	{
		public int WatchlistId { get; set; }
		public int UserId { get; set; }
		public int AuctionId { get; set; }
		public DateTime? AddedAt { get; set; }

		// Auction summary
		public string ItemTitle { get; set; } = null!;
		public decimal StartingBid { get; set; }
		public decimal? CurrentBid { get; set; }
		public decimal? BuyNowPrice { get; set; }
		public DateTime EndTime { get; set; }
		public string Status { get; set; } = null!;

        public string? ItemImages { get; set; }
        public string? CategoryName { get; set; }
        public int? BidCount { get; set; }
    }
}

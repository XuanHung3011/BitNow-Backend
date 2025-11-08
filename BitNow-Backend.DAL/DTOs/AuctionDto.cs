using System;

namespace BitNow_Backend.DAL.DTOs
{
	public class AuctionDetailDto
	{
		public int Id { get; set; }
		public int ItemId { get; set; }
		public string ItemTitle { get; set; } = null!;
		public string? ItemDescription { get; set; }
		public string? ItemImages { get; set; }
		public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int SellerId { get; set; }
		public decimal StartingBid { get; set; }
		public decimal? CurrentBid { get; set; }
		public decimal? BuyNowPrice { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Status { get; set; } = null!;
		public int? BidCount { get; set; }
	}
}

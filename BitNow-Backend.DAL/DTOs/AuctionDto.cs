using System;
using System.Collections.Generic;

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
        public int? SellerTotalRatings { get; set; }
        public string? SellerName { get; set; }
        public decimal StartingBid { get; set; }
		public decimal? CurrentBid { get; set; }
		public decimal? BuyNowPrice { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Status { get; set; } = null!;
		public int? BidCount { get; set; }
	}

	public class AuctionListItemDto
	{
		public int Id { get; set; }
		public string ItemTitle { get; set; } = null!;
		public string? SellerName { get; set; }
		public string? CategoryName { get; set; }
		public decimal StartingBid { get; set; }
		public decimal? CurrentBid { get; set; }
		public DateTime EndTime { get; set; }
		public string Status { get; set; } = null!;
		public string DisplayStatus { get; set; } = null!; // active, scheduled, completed, suspended
		public int? BidCount { get; set; }
	}

	public class AuctionFilterDto
	{
		public string? SearchTerm { get; set; }
		public List<string>? Statuses { get; set; } // active, scheduled, completed, suspended
		public string? SortBy { get; set; } = "EndTime"; // ItemTitle, EndTime, CurrentBid, BidCount
		public string? SortOrder { get; set; } = "desc"; // asc, desc
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 10;
	}

    public class CreateAuctionDto
    {
        public int ItemId { get; set; }
        public int SellerId { get; set; }
        public decimal StartingBid { get; set; }
        public decimal? BuyNowPrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class AuctionResponseDto
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
        public DateTime? CreatedAt { get; set; }
    }
    public class BuyerActiveBidDto
    {
        public int AuctionId { get; set; }
        public string ItemTitle { get; set; } = null!;
        public string? ItemImages { get; set; }
        public string? CategoryName { get; set; }
        public decimal CurrentBid { get; set; }
        public decimal YourHighestBid { get; set; }
        public bool IsLeading { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalBids { get; set; }
        public int YourBidCount { get; set; }
    }

    public class BuyerWonAuctionDto
    {
        public int AuctionId { get; set; }
        public string ItemTitle { get; set; } = null!;
        public string? ItemImages { get; set; }
        public string? CategoryName { get; set; }
        public decimal FinalBid { get; set; }
        public DateTime WonDate { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = null!; // completed
        public string? SellerName { get; set; }
        public int SellerId { get; set; }
        public bool HasRated { get; set; }
    }
}

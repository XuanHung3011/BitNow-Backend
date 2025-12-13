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
		public string? ItemSpecifics { get; set; }
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
		public DateTime? PausedAt { get; set; }
        public int? WinnerId { get; set; }
        public string? WinnerName { get; set; }
	}

	public class AuctionListItemDto
	{
		public int Id { get; set; }
		public string ItemTitle { get; set; } = null!;
		public string? ItemImages { get; set; } // Images from the item
		public string? SellerName { get; set; }
		public string? CategoryName { get; set; }
		public decimal StartingBid { get; set; }
		public decimal? CurrentBid { get; set; }
        public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Status { get; set; } = null!;
		public string DisplayStatus { get; set; } = null!; // active, scheduled, completed, suspended
		public int? BidCount { get; set; }
		public DateTime? PausedAt { get; set; }
	}

	public class AuctionFilterDto
	{
		public string? SearchTerm { get; set; }
		public List<string>? Statuses { get; set; } // active, scheduled, completed, paused
		public int? CategoryId { get; set; } // Filter by category
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
        // Order and Payment information
        public int? OrderId { get; set; }
        public string? OrderStatus { get; set; } // awaiting_payment, awaiting_shipment, shipped, dispute, completed, cancelled
        public string? PaymentStatus { get; set; } // pending, paid_held, hold_dispute, refunded_to_buyer, released_to_seller
        public DateTime? PaidAt { get; set; }
        public bool HasOrder { get; set; }
        public bool HasPayment { get; set; }
    }

    public class SellerAuctionDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string ItemTitle { get; set; } = null!;
        public string? ItemImages { get; set; }
        public string? CategoryName { get; set; }
        public decimal StartingBid { get; set; }
        public decimal? CurrentBid { get; set; }
        public decimal? BuyNowPrice { get; set; }
        public int BidCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = null!;
        public string DisplayStatus { get; set; } = null!; // active, scheduled, completed, draft
        public int? WinnerId { get; set; }
        public string? WinnerName { get; set; }
        public bool HasRated { get; set; }
    }

    public class BuyNowRequestDto
    {
        public int BuyerId { get; set; }
    }

    public class AuctionCompletionResultDto
    {
        public int AuctionId { get; set; }
        public int? WinnerId { get; set; }
        public decimal? FinalPrice { get; set; }
        public string Status { get; set; } = "completed";
        public string CompletionType { get; set; } = "timeout"; // timeout | buy-now | manual
        public DateTime CompletedAt { get; set; }
    }
}

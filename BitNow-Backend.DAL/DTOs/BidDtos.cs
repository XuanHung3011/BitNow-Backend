namespace BitNow_Backend.DAL.DTOs
{
	public class BidDto
	{
		public int BidderId { get; set; }
		public string? BidderName { get; set; }
		public decimal Amount { get; set; }
		public DateTime BidTime { get; set; }
	}

	public class BidRequestDto
	{
		public int BidderId { get; set; }
		public decimal Amount { get; set; }
	}

	public class BidResultDto
	{
		public int AuctionId { get; set; }
		public decimal CurrentBid { get; set; }
		public int BidCount { get; set; }
		public BidDto PlacedBid { get; set; } = new();
	}

    public class BiddingHistoryDto
    {
        public int BidId { get; set; }
        public int AuctionId { get; set; }
        public string ItemTitle { get; set; } = string.Empty;
        public string? ItemImages { get; set; }
        public string? CategoryName { get; set; }
        public decimal YourBid { get; set; }
        public DateTime BidTime { get; set; }
        public string Status { get; set; } = string.Empty; // "leading", "outbid", "won", "lost"
        public decimal? CurrentBid { get; set; }
        public DateTime? EndTime { get; set; }
        public string? AuctionStatus { get; set; }
        public bool IsAutoBid { get; set; }
    }

    public class PaginatedResultB<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}



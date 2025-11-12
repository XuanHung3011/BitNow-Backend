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
}



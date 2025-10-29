using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.DAL.DTOs
{
    public class ItemResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? BasePrice { get; set; }
        public string? Condition { get; set; }
        public string? Images { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Category Info
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }
        public string? CategoryIcon { get; set; }

        // Seller Info
        public int SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? SellerEmail { get; set; }
        public string? SellerAvatar { get; set; }
        public decimal? SellerReputationScore { get; set; }
        public int? SellerTotalSales { get; set; }

        // Auction Info (NEW)
        public int? AuctionId { get; set; }
        public decimal? StartingBid { get; set; }  // Giá khởi điểm
        public decimal? CurrentBid { get; set; }   // Giá hiện tại
        public int? BidCount { get; set; }         // Số lượt đấu giá
        public DateTime? AuctionEndTime { get; set; }  // Thời gian kết thúc
        public string? AuctionStatus { get; set; }     // Trạng thái đấu giá
    }

    public class ItemSearchDto
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Condition { get; set; }
    }
}

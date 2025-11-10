using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.DAL.DTOs
{
    public class FavoriteSellerDto
    {
        public int Id { get; set; }
        public int BuyerId { get; set; }
        public int SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? SellerEmail { get; set; }
        public string? SellerAvatarUrl { get; set; }
        public decimal? SellerReputationScore { get; set; }
        public int? SellerTotalSales { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AddFavoriteSellerDto
    {
        public int SellerId { get; set; }
    }

    public class FavoriteSellerResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public FavoriteSellerDto? Data { get; set; }
    }
}

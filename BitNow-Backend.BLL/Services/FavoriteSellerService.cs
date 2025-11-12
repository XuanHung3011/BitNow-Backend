using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.BLL.Services
{
    public class FavoriteSellerService : IFavoriteSellerService
    {
        private readonly IFavoriteSellerRepository _favoriteSellerRepository;

        public FavoriteSellerService(IFavoriteSellerRepository favoriteSellerRepository)
        {
            _favoriteSellerRepository = favoriteSellerRepository;
        }

        public async Task<FavoriteSellerResponseDto> AddFavoriteAsync(int buyerId, int sellerId)
        {
            // Check if buyer and seller are the same
            if (buyerId == sellerId)
            {
                return new FavoriteSellerResponseDto
                {
                    Success = false,
                    Message = "Bạn không thể thêm chính mình vào danh sách yêu thích"
                };
            }

            // Check if already exists
            var exists = await _favoriteSellerRepository.ExistsAsync(buyerId, sellerId);
            if (exists)
            {
                return new FavoriteSellerResponseDto
                {
                    Success = false,
                    Message = "Người bán này đã có trong danh sách yêu thích của bạn"
                };
            }

            // Add to favorites
            var favoriteSeller = new FavoriteSeller
            {
                BuyerId = buyerId,
                SellerId = sellerId,
                CreatedAt = DateTime.UtcNow
            };

            var added = await _favoriteSellerRepository.AddAsync(favoriteSeller);

            return new FavoriteSellerResponseDto
            {
                Success = true,
                Message = "Đã thêm người bán vào danh sách yêu thích",
                Data = MapToDto(added)
            };
        }

        public async Task<FavoriteSellerResponseDto> RemoveFavoriteAsync(int buyerId, int sellerId)
        {
            var deleted = await _favoriteSellerRepository.DeleteAsync(buyerId, sellerId);

            if (!deleted)
            {
                return new FavoriteSellerResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người bán trong danh sách yêu thích"
                };
            }

            return new FavoriteSellerResponseDto
            {
                Success = true,
                Message = "Đã xóa người bán khỏi danh sách yêu thích"
            };
        }

        public async Task<List<FavoriteSellerDto>> GetFavoritesAsync(int buyerId)
        {
            var favorites = await _favoriteSellerRepository.GetByBuyerIdAsync(buyerId);
            return favorites.Select(MapToDto).ToList();
        }

        public async Task<bool> IsFavoriteAsync(int buyerId, int sellerId)
        {
            return await _favoriteSellerRepository.ExistsAsync(buyerId, sellerId);
        }

        private FavoriteSellerDto MapToDto(FavoriteSeller fs)
        {
            return new FavoriteSellerDto
            {
                Id = fs.Id,
                BuyerId = fs.BuyerId,
                SellerId = fs.SellerId,
                SellerName = fs.Seller?.FullName,
                SellerEmail = fs.Seller?.Email,
                SellerAvatarUrl = fs.Seller?.AvatarUrl,
                SellerReputationScore = fs.Seller?.ReputationScore,
                SellerTotalSales = fs.Seller?.TotalSales,
                CreatedAt = fs.CreatedAt
            };
        }
    }
}

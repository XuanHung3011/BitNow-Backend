using BitNow_Backend.DAL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.BLL.IServices
{
    public interface IFavoriteSellerService
    {
        Task<FavoriteSellerResponseDto> AddFavoriteAsync(int buyerId, int sellerId);
        Task<FavoriteSellerResponseDto> RemoveFavoriteAsync(int buyerId, int sellerId);
        Task<List<FavoriteSellerDto>> GetFavoritesAsync(int buyerId);
        Task<bool> IsFavoriteAsync(int buyerId, int sellerId);
    }
}

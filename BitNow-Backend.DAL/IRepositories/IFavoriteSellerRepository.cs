using BitNow_Backend.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.DAL.IRepositories
{

    public interface IFavoriteSellerRepository
    {
        Task<FavoriteSeller?> GetByBuyerAndSellerAsync(int buyerId, int sellerId);
        Task<List<FavoriteSeller>> GetByBuyerIdAsync(int buyerId);
        Task<FavoriteSeller> AddAsync(FavoriteSeller favoriteSeller);
        Task<bool> DeleteAsync(int buyerId, int sellerId);
        Task<bool> ExistsAsync(int buyerId, int sellerId);
    }
}

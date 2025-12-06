using BitNow_Backend.DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitNow_Backend.DAL.IRepositories
{
    public interface IUserAuctionViewRepository
    {

        Task<UserAuctionView?> GetLastViewAsync(int userId, int auctionId);


        Task<List<UserAuctionView>> GetRecentByUserAsync(int userId, int take = 20);



        Task AddAsync(UserAuctionView view);
    }
}




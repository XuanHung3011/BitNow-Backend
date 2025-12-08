using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitNow_Backend.BLL.Services
{
    public class UserAuctionViewService : IUserAuctionViewService
    {
        private readonly IUserAuctionViewRepository _repository;
        private readonly ILogger<UserAuctionViewService> _logger;

        public UserAuctionViewService(
            IUserAuctionViewRepository repository,
            ILogger<UserAuctionViewService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task LogViewAsync(int userId, int auctionId, int intervalMinutes = 3, CancellationToken cancellationToken = default)
        {
            if (userId <= 0 || auctionId <= 0) return;
            if (intervalMinutes <= 0) intervalMinutes = 3;

            try
            {
                var now = DateTime.UtcNow;

                // Lấy lần xem gần nhất của USER này trên AUCTION này
                var lastView = await _repository.GetLastViewAsync(userId, auctionId);

                if (lastView != null)
                {
                    var diffMinutes = (now - lastView.ViewedAt).TotalMinutes;
                    if (diffMinutes < intervalMinutes)
                    {
                        // Bỏ qua: User vừa xem auction này cách đây < 3 phút
                        _logger.LogDebug(
                            "Skip logging view: User {UserId} viewed Auction {AuctionId} {Minutes:F1} minutes ago (< {Interval} min)",
                            userId, auctionId, diffMinutes, intervalMinutes);
                        return;
                    }
                }

                // Log view mới
                var view = new UserAuctionView
                {
                    UserId = userId,
                    AuctionId = auctionId,
                    ViewedAt = now
                };

                await _repository.AddAsync(view);

                _logger.LogInformation(
                    "Logged auction view: User {UserId} -> Auction {AuctionId}",
                    userId, auctionId);
            }
            catch (Exception ex)
            {
                
                _logger.LogWarning(ex,
                    "Failed to log auction view for User {UserId}, Auction {AuctionId}",
                    userId, auctionId);
            }
        }

        public async Task<IReadOnlyList<int>> GetRecentViewedAuctionIdsAsync(int userId, int take = 20, CancellationToken cancellationToken = default)
        {
            if (userId <= 0) return Array.Empty<int>();
            if (take <= 0) take = 20;

            var views = await _repository.GetRecentByUserAsync(userId, take);

            // Ưu tiên các auction xem gần đây, loại trùng
            var ids = views
                .OrderByDescending(v => v.ViewedAt)
                .Select(v => v.AuctionId)
                .ToList();

            return ids;
        }
    }
}


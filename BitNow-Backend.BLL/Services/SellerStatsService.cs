using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.BLL.Services;

public class SellerStatsService : ISellerStatsService
{
    private readonly BidNowDbContext _context;

    public SellerStatsService(BidNowDbContext context)
    {
        _context = context;
    }

    public async Task<SellerStatsDto> GetSellerStatsAsync(int sellerId)
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth.AddDays(-1);

        // Active auctions (status = "active" and currently running)
        var activeAuctions = await _context.Auctions
            .Where(a => a.SellerId == sellerId &&
                       a.Status != null && 
                       a.Status.ToLower() == "active" &&
                       a.StartTime <= now &&
                       a.EndTime > now)
            .CountAsync();

        // Ending soon (within 24 hours)
        var endingSoonAuctions = await _context.Auctions
            .Where(a => a.SellerId == sellerId &&
                       a.Status != null && 
                       a.Status.ToLower() == "active" &&
                       a.StartTime <= now &&
                       a.EndTime > now &&
                       a.EndTime <= now.AddHours(24))
            .CountAsync();

        // Total listings (all auctions by seller)
        var totalListings = await _context.Auctions
            .Where(a => a.SellerId == sellerId)
            .CountAsync();

        // Completed auctions (ended or status = "completed")
        var completedAuctions = await _context.Auctions
            .Where(a => a.SellerId == sellerId &&
                       (a.EndTime <= now || (a.Status != null && a.Status.ToLower() == "completed")))
            .CountAsync();

        // Revenue this month (from completed auctions with winner)
        var revenueThisMonth = await _context.Auctions
            .Where(a => a.SellerId == sellerId &&
                       a.WinnerId != null &&
                       a.CurrentBid != null &&
                       a.EndTime >= startOfMonth && 
                       a.EndTime < startOfMonth.AddMonths(1))
            .SumAsync(a => a.CurrentBid ?? 0);

        // Revenue last month
        var revenueLastMonth = await _context.Auctions
            .Where(a => a.SellerId == sellerId &&
                       a.WinnerId != null &&
                       a.CurrentBid != null &&
                       a.EndTime >= startOfLastMonth && 
                       a.EndTime < startOfMonth)
            .SumAsync(a => a.CurrentBid ?? 0);

        // Calculate revenue change percent
        var revenueChangePercent = revenueLastMonth > 0
            ? ((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100
            : (revenueThisMonth > 0 ? 100 : 0);

        // Total bids on seller's auctions
        var totalBids = await _context.Bids
            .Where(b => b.Auction.SellerId == sellerId)
            .CountAsync();

        // Average rating (from Ratings where RatedId == sellerId)
        var ratings = await _context.Ratings
            .Where(r => r.RatedId == sellerId)
            .ToListAsync();

        var averageRating = ratings.Any() ? (decimal)ratings.Average(r => r.Rating1) : 0;
        var totalRatings = ratings.Count;

        return new SellerStatsDto
        {
            ActiveAuctions = activeAuctions,
            EndingSoonAuctions = endingSoonAuctions,
            TotalListings = totalListings,
            CompletedAuctions = completedAuctions,
            RevenueThisMonth = revenueThisMonth,
            RevenueLastMonth = revenueLastMonth,
            RevenueChangePercent = revenueChangePercent,
            TotalBids = totalBids,
            AverageRating = averageRating,
            TotalRatings = totalRatings
        };
    }

    public async Task<SellerStatsDetailDto> GetSellerStatsDetailAsync(int sellerId, string type)
    {
        var now = DateTime.Now;
        var chartData = new List<ChartDataPoint>();
        var summary = new Dictionary<string, object>();

        switch (type.ToLower())
        {
            case "auctions":
                // Get auction creation data for last 30 days
                for (int i = 29; i >= 0; i--)
                {
                    var date = now.AddDays(-i).Date;
                    var nextDate = date.AddDays(1);

                    var count = await _context.Auctions
                        .Where(a => a.SellerId == sellerId &&
                                   a.CreatedAt.HasValue &&
                                   a.CreatedAt.Value >= date && 
                                   a.CreatedAt.Value < nextDate)
                        .CountAsync();

                    chartData.Add(new ChartDataPoint
                    {
                        Name = date.ToString("dd/MM"),
                        Value = count
                    });
                }
                break;

            case "revenue":
                // Get revenue data for last 30 days
                for (int i = 29; i >= 0; i--)
                {
                    var date = now.AddDays(-i).Date;
                    var nextDate = date.AddDays(1);

                    var revenue = await _context.Auctions
                        .Where(a => a.SellerId == sellerId &&
                                   a.WinnerId != null &&
                                   a.CurrentBid != null &&
                                   a.EndTime >= date && 
                                   a.EndTime < nextDate)
                        .SumAsync(a => a.CurrentBid ?? 0);

                    chartData.Add(new ChartDataPoint
                    {
                        Name = date.ToString("dd/MM"),
                        Value = revenue
                    });
                }
                break;

            case "bids":
                // Get bid data for last 30 days
                for (int i = 29; i >= 0; i--)
                {
                    var date = now.AddDays(-i).Date;
                    var nextDate = date.AddDays(1);

                    var count = await _context.Bids
                        .Where(b => b.Auction.SellerId == sellerId &&
                                   b.BidTime.HasValue &&
                                   b.BidTime.Value >= date && 
                                   b.BidTime.Value < nextDate)
                        .CountAsync();

                    chartData.Add(new ChartDataPoint
                    {
                        Name = date.ToString("dd/MM"),
                        Value = count
                    });
                }
                break;
        }

        return new SellerStatsDetailDto
        {
            ChartData = chartData,
            Summary = summary
        };
    }
}


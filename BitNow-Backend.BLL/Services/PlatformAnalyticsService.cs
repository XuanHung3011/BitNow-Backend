using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.BLL.Services;

public class PlatformAnalyticsService : IPlatformAnalyticsService
{
    private readonly BidNowDbContext _context;

    public PlatformAnalyticsService(BidNowDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformAnalyticsDto> GetPlatformAnalyticsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth;
        var startOfToday = now.Date;
        var endOfToday = startOfToday.AddDays(1);
        var yesterday = startOfToday.AddDays(-1);

        // New users this month vs last month
        var newUsersThisMonth = await _context.Users
            .Where(u => u.CreatedAt >= startOfMonth && u.CreatedAt < endOfMonth)
            .CountAsync();

        var newUsersLastMonth = await _context.Users
            .Where(u => u.CreatedAt >= startOfLastMonth && u.CreatedAt < endOfLastMonth)
            .CountAsync();

        var newUsersChangePercent = newUsersLastMonth > 0
            ? ((newUsersThisMonth - newUsersLastMonth) / (decimal)newUsersLastMonth) * 100
            : (newUsersThisMonth > 0 ? 100 : 0);

        // New auctions this month vs last month (auctions created)
        var newAuctionsThisMonth = await _context.Auctions
            .Where(a => a.CreatedAt >= startOfMonth && a.CreatedAt < endOfMonth)
            .CountAsync();

        var newAuctionsLastMonth = await _context.Auctions
            .Where(a => a.CreatedAt >= startOfLastMonth && a.CreatedAt < endOfLastMonth)
            .CountAsync();

        var newAuctionsChangePercent = newAuctionsLastMonth > 0
            ? ((newAuctionsThisMonth - newAuctionsLastMonth) / (decimal)newAuctionsLastMonth) * 100
            : (newAuctionsThisMonth > 0 ? 100 : 0);

        // Total transactions (revenue) this month vs last month
        var revenueThisMonth = await _context.Auctions
            .Where(a => a.WinnerId != null &&
                a.CurrentBid != null &&
                a.EndTime >= startOfMonth && a.EndTime < endOfMonth)
            .SumAsync(a => a.CurrentBid ?? 0);

        var revenueLastMonth = await _context.Auctions
            .Where(a => a.WinnerId != null &&
                a.CurrentBid != null &&
                a.EndTime >= startOfLastMonth && a.EndTime < endOfLastMonth)
            .SumAsync(a => a.CurrentBid ?? 0);

        var revenueChangePercent = revenueLastMonth > 0
            ? ((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100
            : (revenueThisMonth > 0 ? 100 : 0);

        // Success rate (completed auctions with winner / total auctions ended)
        var completedAuctionsThisMonth = await _context.Auctions
            .Where(a => a.EndTime >= startOfMonth && a.EndTime < endOfMonth &&
                a.WinnerId != null)
            .CountAsync();

        var totalEndedAuctionsThisMonth = await _context.Auctions
            .Where(a => a.EndTime >= startOfMonth && a.EndTime < endOfMonth)
            .CountAsync();

        var successRateThisMonth = totalEndedAuctionsThisMonth > 0
            ? (completedAuctionsThisMonth / (decimal)totalEndedAuctionsThisMonth) * 100
            : 0;

        var completedAuctionsLastMonth = await _context.Auctions
            .Where(a => a.EndTime >= startOfLastMonth && a.EndTime < endOfLastMonth &&
                a.WinnerId != null)
            .CountAsync();

        var totalEndedAuctionsLastMonth = await _context.Auctions
            .Where(a => a.EndTime >= startOfLastMonth && a.EndTime < endOfLastMonth)
            .CountAsync();

        var successRateLastMonth = totalEndedAuctionsLastMonth > 0
            ? (completedAuctionsLastMonth / (decimal)totalEndedAuctionsLastMonth) * 100
            : 0;

        var successRateChangePercent = successRateLastMonth > 0
            ? successRateThisMonth - successRateLastMonth
            : (successRateThisMonth > 0 ? successRateThisMonth : 0);

        // Top categories by revenue and auction count
        var topCategories = await _context.Categories
            .Select(c => new
            {
                Category = c,
                Auctions = _context.Auctions
                    .Count(a => a.Item != null && a.Item.CategoryId == c.Id &&
                        a.EndTime >= startOfMonth && a.EndTime < endOfMonth),
                Revenue = _context.Auctions
                    .Where(a => a.Item != null && a.Item.CategoryId == c.Id &&
                        a.WinnerId != null && a.CurrentBid != null &&
                        a.EndTime >= startOfMonth && a.EndTime < endOfMonth)
                    .Sum(a => a.CurrentBid ?? 0)
            })
            .Where(x => x.Auctions > 0)
            .OrderByDescending(x => x.Revenue)
            .Take(4)
            .Select(x => new TopCategoryDto
            {
                Name = x.Category.Name,
                Auctions = x.Auctions,
                Revenue = x.Revenue
            })
            .ToListAsync();

        // Recent activity
        // Online users: users who were active in last 24 hours (approximate: users who created items/auctions/bids in last 24h)
        var activeUsersLast24h = await _context.Users
            .Where(u => 
                (u.Items.Any(i => i.CreatedAt >= yesterday) ||
                 u.AuctionSellers.Any(a => a.CreatedAt >= yesterday) ||
                 u.Bids.Any(b => b.BidTime >= yesterday)))
            .Select(u => u.Id)
            .Distinct()
            .CountAsync();

        var activeAuctions = await _context.Auctions
            .Where(a => a.Status != null && a.Status.ToLower() == "active" &&
                a.StartTime <= now && a.EndTime > now)
            .CountAsync();

        var bidsToday = await _context.Bids
            .Where(b => b.BidTime >= startOfToday && b.BidTime < endOfToday)
            .CountAsync();

        var transactionsToday = await _context.Auctions
            .Where(a => a.WinnerId != null &&
                a.CurrentBid != null &&
                a.EndTime >= startOfToday && a.EndTime < endOfToday)
            .SumAsync(a => a.CurrentBid ?? 0);

        // System alerts
        var pendingItems = await _context.Items
            .Where(i => i.Status != null && i.Status.ToLower() == "pending")
            .CountAsync();

        var processingDisputes = await _context.ContactMessages
            .Where(c => c.Status != null &&
                (c.Status.ToLower() == "pending" || c.Status.ToLower() == "processing"))
            .CountAsync();

        var urgentDisputes = await _context.ContactMessages
            .Where(c => c.Status != null &&
                (c.Status.ToLower() == "pending" || c.Status.ToLower() == "processing") &&
                c.CreatedAt.HasValue && c.CreatedAt.Value < now.AddDays(-3))
            .CountAsync();

        var systemStatus = "normal";
        var systemStatusMessage = "Hệ thống hoạt động bình thường";
        if (urgentDisputes > 0)
        {
            systemStatus = "error";
            systemStatusMessage = "Có tranh chấp khẩn cấp cần xử lý";
        }
        else if (pendingItems > 20 || processingDisputes > 10)
        {
            systemStatus = "warning";
            systemStatusMessage = "Có nhiều yêu cầu chờ xử lý";
        }

        return new PlatformAnalyticsDto
        {
            NewUsers = new MonthlyMetricDto
            {
                Current = newUsersThisMonth,
                Previous = newUsersLastMonth,
                ChangePercent = newUsersChangePercent
            },
            NewAuctions = new MonthlyMetricDto
            {
                Current = newAuctionsThisMonth,
                Previous = newAuctionsLastMonth,
                ChangePercent = newAuctionsChangePercent
            },
            TotalTransactions = new MonthlyMetricDto
            {
                Current = (long)revenueThisMonth,
                Previous = (long)revenueLastMonth,
                ChangePercent = revenueChangePercent
            },
            SuccessRate = new MonthlyMetricDto
            {
                Current = (long)Math.Round(successRateThisMonth), // Already in percentage (0-100)
                Previous = (long)Math.Round(successRateLastMonth),
                ChangePercent = successRateChangePercent // Already in percentage points
            },
            TopCategories = topCategories,
            RecentActivity = new RecentActivityDto
            {
                OnlineUsers = activeUsersLast24h,
                ActiveAuctions = activeAuctions,
                BidsToday = bidsToday,
                TransactionsToday = transactionsToday
            },
            SystemAlerts = new SystemAlertsDto
            {
                SystemStatus = systemStatus,
                SystemStatusMessage = systemStatusMessage,
                PendingAuctions = pendingItems,
                ProcessingDisputes = processingDisputes,
                UrgentDisputes = urgentDisputes
            }
        };
    }

    public async Task<PlatformAnalyticsDetailDto> GetAnalyticsDetailAsync(string type)
    {
        var now = DateTime.UtcNow;
        var chartData = new List<ChartDataPoint>();
        var summary = new Dictionary<string, object>();

        switch (type.ToLower())
        {
            case "newusers":
                // Get new users data for last 12 weeks
                for (int i = 11; i >= 0; i--)
                {
                    var weekStart = now.AddDays(-(i * 7)).Date;
                    var weekEnd = weekStart.AddDays(7);
                    var count = await _context.Users
                        .Where(u => u.CreatedAt >= weekStart && u.CreatedAt < weekEnd)
                        .CountAsync();
                    
                    chartData.Add(new ChartDataPoint
                    {
                        Name = $"Tuần {12 - i}",
                        Value = count
                    });
                }
                
                // Summary
                var newUsersThisMonth = await _context.Users
                    .Where(u => u.CreatedAt >= new DateTime(now.Year, now.Month, 1))
                    .CountAsync();
                var newUsersLastMonth = await _context.Users
                    .Where(u => u.CreatedAt >= new DateTime(now.Year, now.Month, 1).AddMonths(-1) && 
                        u.CreatedAt < new DateTime(now.Year, now.Month, 1))
                    .CountAsync();
                var totalUsers = await _context.Users.CountAsync();
                
                summary["Người dùng mới tháng này"] = newUsersThisMonth;
                summary["Người dùng mới tháng trước"] = newUsersLastMonth;
                summary["Tổng người dùng"] = totalUsers;
                break;

            case "newauctions":
                // Get new auctions data for last 12 weeks
                for (int i = 11; i >= 0; i--)
                {
                    var weekStart = now.AddDays(-(i * 7)).Date;
                    var weekEnd = weekStart.AddDays(7);
                    var count = await _context.Auctions
                        .Where(a => a.CreatedAt >= weekStart && a.CreatedAt < weekEnd)
                        .CountAsync();
                    
                    chartData.Add(new ChartDataPoint
                    {
                        Name = $"Tuần {12 - i}",
                        Value = count
                    });
                }
                
                // Summary
                var newAuctionsThisMonth = await _context.Auctions
                    .Where(a => a.CreatedAt >= new DateTime(now.Year, now.Month, 1))
                    .CountAsync();
                var newAuctionsLastMonth = await _context.Auctions
                    .Where(a => a.CreatedAt >= new DateTime(now.Year, now.Month, 1).AddMonths(-1) && 
                        a.CreatedAt < new DateTime(now.Year, now.Month, 1))
                    .CountAsync();
                var totalAuctions = await _context.Auctions.CountAsync();
                
                summary["Phiên đấu giá mới tháng này"] = newAuctionsThisMonth;
                summary["Phiên đấu giá mới tháng trước"] = newAuctionsLastMonth;
                summary["Tổng số phiên đấu giá"] = totalAuctions;
                break;

            case "totaltransactions":
                // Get revenue data for last 12 months
                for (int i = 11; i >= 0; i--)
                {
                    var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1);
                    var revenue = await _context.Auctions
                        .Where(a => a.WinnerId != null &&
                            a.CurrentBid != null &&
                            a.EndTime >= monthStart && a.EndTime < monthEnd)
                        .SumAsync(a => a.CurrentBid ?? 0);
                    
                    chartData.Add(new ChartDataPoint
                    {
                        Name = monthStart.ToString("MM/yyyy"),
                        Value = revenue
                    });
                }
                
                // Summary
                var revenueThisMonth = await _context.Auctions
                    .Where(a => a.WinnerId != null &&
                        a.CurrentBid != null &&
                        a.EndTime >= new DateTime(now.Year, now.Month, 1) && 
                        a.EndTime < new DateTime(now.Year, now.Month, 1).AddMonths(1))
                    .SumAsync(a => a.CurrentBid ?? 0);
                var revenueLastMonth = await _context.Auctions
                    .Where(a => a.WinnerId != null &&
                        a.CurrentBid != null &&
                        a.EndTime >= new DateTime(now.Year, now.Month, 1).AddMonths(-1) && 
                        a.EndTime < new DateTime(now.Year, now.Month, 1))
                    .SumAsync(a => a.CurrentBid ?? 0);
                var totalRevenue = await _context.Auctions
                    .Where(a => a.WinnerId != null && a.CurrentBid != null)
                    .SumAsync(a => a.CurrentBid ?? 0);
                
                summary["Doanh thu tháng này"] = revenueThisMonth;
                summary["Doanh thu tháng trước"] = revenueLastMonth;
                summary["Tổng doanh thu"] = totalRevenue;
                break;

            case "successrate":
                // Get success rate data for last 12 months
                for (int i = 11; i >= 0; i--)
                {
                    var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1);
                    var completed = await _context.Auctions
                        .Where(a => a.EndTime >= monthStart && a.EndTime < monthEnd && a.WinnerId != null)
                        .CountAsync();
                    var total = await _context.Auctions
                        .Where(a => a.EndTime >= monthStart && a.EndTime < monthEnd)
                        .CountAsync();
                    var rate = total > 0 ? (completed / (decimal)total) * 100 : 0;
                    
                    chartData.Add(new ChartDataPoint
                    {
                        Name = monthStart.ToString("MM/yyyy"),
                        Value = (decimal)Math.Round(rate, 1)
                    });
                }
                
                // Summary
                var completedThisMonth = await _context.Auctions
                    .Where(a => a.EndTime >= new DateTime(now.Year, now.Month, 1) && 
                        a.EndTime < new DateTime(now.Year, now.Month, 1).AddMonths(1) && 
                        a.WinnerId != null)
                    .CountAsync();
                var totalThisMonth = await _context.Auctions
                    .Where(a => a.EndTime >= new DateTime(now.Year, now.Month, 1) && 
                        a.EndTime < new DateTime(now.Year, now.Month, 1).AddMonths(1))
                    .CountAsync();
                var rateThisMonth = totalThisMonth > 0 ? (completedThisMonth / (decimal)totalThisMonth) * 100 : 0;
                
                summary["Tỷ lệ thành công tháng này"] = $"{Math.Round(rateThisMonth, 1)}%";
                summary["Phiên hoàn thành"] = completedThisMonth;
                summary["Tổng phiên kết thúc"] = totalThisMonth;
                break;

            default:
                // Return empty data for unknown types
                break;
        }

        return new PlatformAnalyticsDetailDto
        {
            ChartData = chartData,
            Summary = summary
        };
    }
}


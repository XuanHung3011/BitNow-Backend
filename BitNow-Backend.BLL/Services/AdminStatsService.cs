using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.BLL.Services;

public class AdminStatsService : IAdminStatsService
{
    private readonly BidNowDbContext _context;

    public AdminStatsService(BidNowDbContext context)
    {
        _context = context;
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek).Date;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth.AddDays(-1);

        // Total users
        var totalUsers = await _context.Users.CountAsync();

        // New users this week
        var newUsersThisWeek = await _context.Users
            .Where(u => u.CreatedAt >= startOfWeek)
            .CountAsync();

        // Active auctions (status = "active" and end time hasn't passed)
        var activeAuctions = await _context.Auctions
            .Where(a => a.Status != null && a.Status.ToLower() == "active" && a.EndTime > now)
            .CountAsync();

        // Pending items (status = "pending")
        var pendingItems = await _context.Items
            .Where(i => i.Status != null && i.Status.ToLower() == "pending")
            .CountAsync();

        // Disputes processing (ContactMessages with status "pending" or "processing")
        var disputesProcessing = await _context.ContactMessages
            .Where(c => c.Status != null && 
                (c.Status.ToLower() == "pending" || c.Status.ToLower() == "processing"))
            .CountAsync();

        // Urgent disputes (disputes created more than 3 days ago that are still pending/processing)
        var urgentDisputes = await _context.ContactMessages
            .Where(c => c.Status != null && 
                (c.Status.ToLower() == "pending" || c.Status.ToLower() == "processing") &&
                c.CreatedAt.HasValue && c.CreatedAt.Value < now.AddDays(-3))
            .CountAsync();

        // Revenue this month (from auctions that ended this month with a winner - use CurrentBid as final bid)
        // Check for auctions that ended in the month range and have a winner (regardless of status)
        var revenueThisMonth = await _context.Auctions
            .Where(a => a.WinnerId != null &&
                a.CurrentBid != null &&
                a.EndTime >= startOfMonth && a.EndTime < startOfMonth.AddMonths(1))
            .SumAsync(a => a.CurrentBid ?? 0);

        // Revenue last month
        var revenueLastMonth = await _context.Auctions
            .Where(a => a.WinnerId != null &&
                a.CurrentBid != null &&
                a.EndTime >= startOfLastMonth && a.EndTime < startOfMonth)
            .SumAsync(a => a.CurrentBid ?? 0);

        // Calculate revenue change percent
        var revenueChangePercent = revenueLastMonth > 0
            ? ((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100
            : (revenueThisMonth > 0 ? 100 : 0);

        return new AdminStatsDto
        {
            TotalUsers = totalUsers,
            NewUsersThisWeek = newUsersThisWeek,
            ActiveAuctions = activeAuctions,
            PendingItems = pendingItems,
            DisputesProcessing = disputesProcessing,
            UrgentDisputes = urgentDisputes,
            RevenueThisMonth = revenueThisMonth,
            RevenueLastMonth = revenueLastMonth,
            RevenueChangePercent = revenueChangePercent
        };
    }

    public async Task<AdminStatsDetailDto> GetAdminStatsDetailAsync(string type)
    {
        var now = DateTime.UtcNow;
        var chartData = new List<ChartDataPoint>();
        var summary = new Dictionary<string, object>();

        switch (type.ToLower())
        {
            case "users":
                // Get user registration data for last 30 days
                for (int i = 29; i >= 0; i--)
                {
                    var date = now.AddDays(-i).Date;
                    var nextDate = date.AddDays(1);
                    var count = await _context.Users
                        .Where(u => u.CreatedAt >= date && u.CreatedAt < nextDate)
                        .CountAsync();
                    
                    chartData.Add(new ChartDataPoint
                    {
                        Name = date.ToString("dd/MM"),
                        Value = count
                    });
                }
                
                // Summary
                var totalUsers = await _context.Users.CountAsync();
                var newUsersThisWeek = await _context.Users
                    .Where(u => u.CreatedAt >= now.AddDays(-7))
                    .CountAsync();
                var newUsersThisMonth = await _context.Users
                    .Where(u => u.CreatedAt >= new DateTime(now.Year, now.Month, 1))
                    .CountAsync();
                
                summary["Tổng người dùng"] = totalUsers;
                summary["Người dùng mới tuần này"] = newUsersThisWeek;
                summary["Người dùng mới tháng này"] = newUsersThisMonth;
                break;

            case "auctions":
                // Get active auctions data for last 30 days
                for (int i = 29; i >= 0; i--)
                {
                    var date = now.AddDays(-i).Date;
                    var count = await _context.Auctions
                        .Where(a => a.Status != null && 
                            a.Status.ToLower() == "active" && 
                            a.StartTime <= date && 
                            a.EndTime > date)
                        .CountAsync();
                    
                    chartData.Add(new ChartDataPoint
                    {
                        Name = date.ToString("dd/MM"),
                        Value = count
                    });
                }
                
                // Summary
                var activeAuctions = await _context.Auctions
                    .Where(a => a.Status != null && a.Status.ToLower() == "active" && a.EndTime > now)
                    .CountAsync();
                var pendingItems = await _context.Items
                    .Where(i => i.Status != null && i.Status.ToLower() == "pending")
                    .CountAsync();
                var totalAuctions = await _context.Auctions.CountAsync();
                
                summary["Phiên đấu giá đang hoạt động"] = activeAuctions;
                summary["Sản phẩm chờ duyệt"] = pendingItems;
                summary["Tổng số phiên đấu giá"] = totalAuctions;
                break;

            case "revenue":
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

            default:
                // Return empty data for unknown types
                break;
        }

        return new AdminStatsDetailDto
        {
            ChartData = chartData,
            Summary = summary
        };
    }
}


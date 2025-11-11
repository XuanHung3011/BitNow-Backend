namespace BitNow_Backend.DAL.DTOs;

public class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int ActiveAuctions { get; set; }
    public int PendingItems { get; set; }
    public int DisputesProcessing { get; set; }
    public int UrgentDisputes { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueLastMonth { get; set; }
    public decimal RevenueChangePercent { get; set; }
}


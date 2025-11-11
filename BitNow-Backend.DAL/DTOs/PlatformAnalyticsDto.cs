namespace BitNow_Backend.DAL.DTOs;

public class PlatformAnalyticsDto
{
    // Monthly metrics with comparison
    public MonthlyMetricDto NewUsers { get; set; } = new();
    public MonthlyMetricDto NewAuctions { get; set; } = new();
    public MonthlyMetricDto TotalTransactions { get; set; } = new();
    public MonthlyMetricDto SuccessRate { get; set; } = new();

    // Top categories
    public List<TopCategoryDto> TopCategories { get; set; } = new();

    // Recent activity
    public RecentActivityDto RecentActivity { get; set; } = new();

    // System alerts
    public SystemAlertsDto SystemAlerts { get; set; } = new();
}

public class MonthlyMetricDto
{
    public long Current { get; set; } // Use long for revenue values
    public long Previous { get; set; }
    public decimal ChangePercent { get; set; }
}

public class TopCategoryDto
{
    public string Name { get; set; } = null!;
    public int Auctions { get; set; }
    public decimal Revenue { get; set; }
}

public class RecentActivityDto
{
    public int OnlineUsers { get; set; } // Users active in last 24 hours
    public int ActiveAuctions { get; set; }
    public int BidsToday { get; set; }
    public decimal TransactionsToday { get; set; }
}

public class SystemAlertsDto
{
    public string SystemStatus { get; set; } = "normal"; // normal, warning, error
    public string SystemStatusMessage { get; set; } = "";
    public int PendingAuctions { get; set; }
    public int ProcessingDisputes { get; set; }
    public int UrgentDisputes { get; set; }
}


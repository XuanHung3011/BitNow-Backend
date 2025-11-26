namespace BitNow_Backend.DAL.DTOs;

public class SellerStatsDto
{
    public int ActiveAuctions { get; set; }
    public int EndingSoonAuctions { get; set; }
    public int TotalListings { get; set; }
    public int CompletedAuctions { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueLastMonth { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public int TotalBids { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
}

public class SellerStatsDetailDto
{
    public List<ChartDataPoint> ChartData { get; set; } = new();
    public Dictionary<string, object>? Summary { get; set; }
}


namespace BitNow_Backend.DAL.DTOs;

public class PlatformAnalyticsDetailDto
{
    public List<ChartDataPoint> ChartData { get; set; } = new();
    public Dictionary<string, object>? Summary { get; set; }
}


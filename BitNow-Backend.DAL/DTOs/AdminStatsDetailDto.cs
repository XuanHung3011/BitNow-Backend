namespace BitNow_Backend.DAL.DTOs;

public class AdminStatsDetailDto
{
    public List<ChartDataPoint> ChartData { get; set; } = new();
    public Dictionary<string, object>? Summary { get; set; }
}

public class ChartDataPoint
{
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
}


using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices;

public interface IPlatformAnalyticsService
{
    Task<PlatformAnalyticsDto> GetPlatformAnalyticsAsync();
}


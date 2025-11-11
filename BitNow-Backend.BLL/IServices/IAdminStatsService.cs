using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices;

public interface IAdminStatsService
{
    Task<AdminStatsDto> GetAdminStatsAsync();
}


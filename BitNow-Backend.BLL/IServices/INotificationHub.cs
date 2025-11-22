using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
    /// <summary>
    /// Abstraction for broadcasting notifications via SignalR
    /// </summary>
    public interface INotificationHub
    {
        Task SendNotificationToUserAsync(int userId, NotificationResponseDto notification);
    }
}


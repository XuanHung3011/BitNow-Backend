using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationResponseDto>> GetNotificationsByUserIdAsync(int userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<NotificationResponseDto>> GetUnreadNotificationsByUserIdAsync(int userId, int page = 1, int pageSize = 20);
        Task<int> GetUnreadCountAsync(int userId);
        Task<NotificationResponseDto> CreateNotificationAsync(CreateNotificationDto dto);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> DeleteNotificationAsync(int notificationId, int userId);
    }
}


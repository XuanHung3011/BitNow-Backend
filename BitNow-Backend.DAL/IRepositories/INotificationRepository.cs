using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(int id);
        Task<IEnumerable<Notification>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(int userId, int page = 1, int pageSize = 20);
        Task<int> GetUnreadCountAsync(int userId);
        Task<Notification> AddAsync(Notification notification);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> DeleteAsync(int notificationId);
    }
}


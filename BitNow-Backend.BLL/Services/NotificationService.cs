using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationHub? _notificationHub;

        public NotificationService(
            INotificationRepository notificationRepository, 
            IUserRepository userRepository,
            INotificationHub? notificationHub = null)
        {
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _notificationHub = notificationHub;
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetNotificationsByUserIdAsync(int userId, int page = 1, int pageSize = 20)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId, page, pageSize);
            return notifications.Select(MapToNotificationResponseDto);
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetUnreadNotificationsByUserIdAsync(int userId, int page = 1, int pageSize = 20)
        {
            var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId, page, pageSize);
            return notifications.Select(MapToNotificationResponseDto);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task<NotificationResponseDto> CreateNotificationAsync(CreateNotificationDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new ArgumentException("User not found");

            var notification = new Notification
            {
                UserId = dto.UserId,
                Type = dto.Type,
                Message = dto.Message,
                Link = dto.Link,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            var savedNotification = await _notificationRepository.AddAsync(notification);
            var notificationDto = MapToNotificationResponseDto(savedNotification);

            // Broadcast notification real-time qua SignalR
            if (_notificationHub != null)
            {
                try
                {
                    await _notificationHub.SendNotificationToUserAsync(dto.UserId, notificationDto);
                }
                catch
                {
                    // Silently fail - broadcast không thành công không ảnh hưởng đến việc tạo notification
                }
            }

            return notificationDto;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
                return false;

            return await _notificationRepository.MarkAsReadAsync(notificationId);
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            return await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
                return false;

            return await _notificationRepository.DeleteAsync(notificationId);
        }

        private static NotificationResponseDto MapToNotificationResponseDto(Notification notification)
        {
            return new NotificationResponseDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Type = notification.Type,
                Message = notification.Message,
                Link = notification.Link,
                IsRead = notification.IsRead ?? false,
                CreatedAt = notification.CreatedAt ?? DateTime.UtcNow
            };
        }
    }
}


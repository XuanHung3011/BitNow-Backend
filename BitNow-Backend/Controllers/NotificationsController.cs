using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all notifications for a user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<NotificationResponseDto>>> GetNotifications(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, page, pageSize);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get unread notifications for a user
        /// </summary>
        [HttpGet("user/{userId}/unread")]
        public async Task<ActionResult<IEnumerable<NotificationResponseDto>>> GetUnreadNotifications(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var notifications = await _notificationService.GetUnreadNotificationsByUserIdAsync(userId, page, pageSize);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notifications for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get unread notification count for a user
        /// </summary>
        [HttpGet("user/{userId}/unread-count")]
        public async Task<ActionResult<UnreadNotificationCountDto>> GetUnreadCount(int userId)
        {
            try
            {
                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Ok(new UnreadNotificationCountDto { Count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create a new notification
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<NotificationResponseDto>> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(dto);
                return CreatedAtAction(nameof(GetNotifications), new { userId = dto.UserId }, notification);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when creating notification: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification: {Message}", ex.Message);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<ActionResult> MarkAsRead(int id, [FromQuery] int userId)
        {
            try
            {
                var result = await _notificationService.MarkAsReadAsync(id, userId);
                if (!result)
                    return NotFound(new { message = "Notification not found or does not belong to user" });

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", id, userId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        [HttpPut("user/{userId}/mark-all-read")]
        public async Task<ActionResult> MarkAllAsRead(int userId)
        {
            try
            {
                var result = await _notificationService.MarkAllAsReadAsync(userId);
                if (!result)
                    return NotFound(new { message = "User not found" });

                return Ok(new { message = "All notifications marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(int id, [FromQuery] int userId)
        {
            try
            {
                var result = await _notificationService.DeleteNotificationAsync(id, userId);
                if (!result)
                    return NotFound(new { message = "Notification not found or does not belong to user" });

                return Ok(new { message = "Notification deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}


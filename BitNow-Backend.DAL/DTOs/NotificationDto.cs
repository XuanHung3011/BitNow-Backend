using System;

namespace BitNow_Backend.DAL.DTOs
{
    public class NotificationResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Type { get; set; }
        public string Message { get; set; } = null!;
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public string? Type { get; set; }
        public string Message { get; set; } = null!;
        public string? Link { get; set; }
    }

    public class UnreadNotificationCountDto
    {
        public int Count { get; set; }
    }
}


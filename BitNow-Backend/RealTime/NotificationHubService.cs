using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace BitNow_Backend.RealTime
{
    public class NotificationHubService : INotificationHub
    {
        private readonly IHubContext<MessageHub> _hubContext;

        public NotificationHubService(IHubContext<MessageHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationToUserAsync(int userId, NotificationResponseDto notification)
        {
            await _hubContext.Clients.Group($"user-{userId}")
                .SendAsync("NotificationReceived", notification);
        }
    }
}


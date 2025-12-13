using Microsoft.AspNetCore.SignalR;
using API.Hubs;
using API.Services.Interfaces;
using DB.DTOs;

namespace API.Services.Realisations;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyUserAsync(Guid userId, ExchangeNotification notification)
    {
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("ReceiveNotification", notification);
    }

    public async Task NotifyBookLikeAsync(Guid userId, BookLikeNotification notification)
    {
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("ReceiveBookLike", notification);
    }

    public async Task NotifyReviewAsync(Guid userId, ReviewNotification notification)
    {
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("ReceiveReview", notification);
    }
}

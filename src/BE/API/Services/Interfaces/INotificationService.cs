using Microsoft.AspNetCore.SignalR;
using API.Hubs;
using DB.DTOs;

namespace API.Services.Interfaces;

public interface INotificationService
{
    Task NotifyUserAsync(Guid userId, ExchangeNotification notification);
    Task NotifyBookLikeAsync(Guid userId, BookLikeNotification notification);
    Task NotifyReviewAsync(Guid userId, ReviewNotification notification);
}

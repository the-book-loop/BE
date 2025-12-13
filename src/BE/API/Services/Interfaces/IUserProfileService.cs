using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface IUserProfileService
{
    Task<Result<UserPublicProfileResponse>> GetUserPublicProfileAsync(Guid userId, Guid? requesterId = null);
    Task<Result<IEnumerable<UserPublicProfileResponse>>> GetExchangeableUsersAsync(Guid currentUserId);
}

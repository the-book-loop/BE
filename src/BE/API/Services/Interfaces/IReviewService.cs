using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface IReviewService
{
    Task<Result<IEnumerable<ReviewResponse>>> GetReviewsAsync(ReviewFilterRequest? request = null);
    Task<Result<ReviewResponse>> GetReviewByIdAsync(Guid reviewId);
    Task<Result<ReviewResponse>> AddReviewAsync(Guid reviewerId, AddReviewRequest request);
    Task<Result<ReviewResponse>> UpdateReviewAsync(Guid reviewId, Guid userId, UpdateReviewRequest request);
    Task<Result> DeleteReviewAsync(Guid reviewId, Guid userId);
    Task<Result<double>> GetUserAverageRatingAsync(Guid userId);
    Task<Result<int>> GetUserReviewCountAsync(Guid userId);
}

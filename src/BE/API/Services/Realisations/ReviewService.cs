using API.Services.Interfaces;
using API.Services.Realisations.Utilites;
using DB.DTOs;
using DB.Models;
using DB.Models.Enums;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace API.Services.Realisations;

public class ReviewService(
    IGenericRepository<Review> reviewRepository,
    IGenericRepository<Exchange> exchangeRepository,
    INotificationService notificationService)
    : IReviewService
{
    public async Task<Result<IEnumerable<ReviewResponse>>> GetReviewsAsync(ReviewFilterRequest? request = null)
    {
        var includes = new List<Func<IQueryable<Review>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Review, object>>>
        {
            query => query
                .Include(r => r.Reviewer)
                .Include(r => r.ReviewedUser)
                .Include(r => r.Exchange)
        };

        Expression<Func<Review, bool>> filter = r => true;

        if (request is not null)
        {
            if (request.ReviewerId.HasValue)
                filter = filter.And(r => r.ReviewerId == request.ReviewerId.Value);
            if (request.ReviewedUserId.HasValue)
                filter = filter.And(r => r.ReviewedUserId == request.ReviewedUserId.Value);
            if (request.ExchangeId.HasValue)
                filter = filter.And(r => r.ExchangeId == request.ExchangeId.Value);
            if (request.MinRating.HasValue)
                filter = filter.And(r => r.Rating >= request.MinRating.Value);
            if (request.MaxRating.HasValue)
                filter = filter.And(r => r.Rating <= request.MaxRating.Value);
        }

        var reviewsResult = await reviewRepository.GetListAsync<Review>(
            filter: filter,
            includes: includes,
            orderBy: q => q.OrderByDescending(r => r.Created),
            selector: null
        );

        if (!reviewsResult.Success)
        {
            return Result<IEnumerable<ReviewResponse>>.Fail(reviewsResult.Error);
        }

        var reviewResponses = reviewsResult.Data.Select(MapToResponse);
        return Result<IEnumerable<ReviewResponse>>.Ok(reviewResponses);
    }

    public async Task<Result<ReviewResponse>> GetReviewByIdAsync(Guid reviewId)
    {
        var includes = new List<Func<IQueryable<Review>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Review, object>>>
        {
            query => query
                .Include(r => r.Reviewer)
                .Include(r => r.ReviewedUser)
                .Include(r => r.Exchange)
        };

        var reviewResult = await reviewRepository.GetSingleAsync<Review>(
            filter: r => r.Id == reviewId,
            includes: includes,
            selector: null
        );

        if (!reviewResult.Success || reviewResult.Data == null)
        {
            return Result<ReviewResponse>.Fail("Review not found");
        }

        return Result<ReviewResponse>.Ok(MapToResponse(reviewResult.Data));
    }

    public async Task<Result<ReviewResponse>> AddReviewAsync(Guid reviewerId, AddReviewRequest request)
    {
        var exchangeIncludes = new List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>
        {
            query => query.Include(e => e.Owner).Include(e => e.Receiver)
        };

        var exchangeResult = await exchangeRepository.GetSingleAsync<Exchange>(
            filter: e => e.Id == request.ExchangeId,
            includes: exchangeIncludes,
            selector: null
        );

        if (!exchangeResult.Success || exchangeResult.Data == null)
        {
            return Result<ReviewResponse>.Fail("Exchange not found");
        }

        var exchange = exchangeResult.Data;

        if (exchange.Status != ExchangeStatus.Accepted)
        {
            return Result<ReviewResponse>.Fail("You can only review completed (accepted) exchanges");
        }

        if (exchange.OwnerId != reviewerId && exchange.ReceiverId != reviewerId)
        {
            return Result<ReviewResponse>.Fail("You are not a participant in this exchange");
        }

        var reviewedUserId = exchange.OwnerId == reviewerId ? exchange.ReceiverId : exchange.OwnerId;

        var existingReviewResult = await reviewRepository.GetSingleAsync<Review>(
            filter: r => r.ExchangeId == request.ExchangeId && r.ReviewerId == reviewerId,
            includes: null,
            selector: null
        );

        if (existingReviewResult.Success && existingReviewResult.Data != null)
        {
            return Result<ReviewResponse>.Fail("You have already reviewed this exchange");
        }

        var review = new Review
        {
            ExchangeId = request.ExchangeId,
            ReviewerId = reviewerId,
            ReviewedUserId = reviewedUserId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        var addResult = await reviewRepository.AddAsync(review);

        if (!addResult.Success)
        {
            return Result<ReviewResponse>.Fail(addResult.Error);
        }

        var addedReview = await GetReviewByIdAsync(review.Id);

        if (addedReview.Success)
        {
            await notificationService.NotifyReviewAsync(reviewedUserId, new ReviewNotification
            {
                Message = "You received a new review",
                Data = addedReview.Data
            });
        }

        return addedReview;
    }

    public async Task<Result<ReviewResponse>> UpdateReviewAsync(Guid reviewId, Guid userId, UpdateReviewRequest request)
    {
        var reviewResult = await reviewRepository.GetSingleAsync<Review>(
            filter: r => r.Id == reviewId,
            includes: null,
            selector: null
        );

        if (!reviewResult.Success || reviewResult.Data == null)
        {
            return Result<ReviewResponse>.Fail("Review not found");
        }

        var review = reviewResult.Data;

        if (review.ReviewerId != userId)
        {
            return Result<ReviewResponse>.Fail("You are not authorized to update this review");
        }

        if (request.Rating.HasValue)
            review.Rating = request.Rating.Value;
        if (request.Comment != null)
            review.Comment = request.Comment;

        review.Modified = DateTime.UtcNow;

        var updateResult = await reviewRepository.UpdateAsync(review);

        if (!updateResult.Success)
        {
            return Result<ReviewResponse>.Fail(updateResult.Error);
        }

        return await GetReviewByIdAsync(reviewId);
    }

    public async Task<Result> DeleteReviewAsync(Guid reviewId, Guid userId)
    {
        var reviewResult = await reviewRepository.GetSingleAsync<Review>(
            filter: r => r.Id == reviewId,
            includes: null,
            selector: null
        );

        if (!reviewResult.Success || reviewResult.Data == null)
        {
            return Result.Fail("Review not found");
        }

        var review = reviewResult.Data;

        if (review.ReviewerId != userId)
        {
            return Result.Fail("You are not authorized to delete this review");
        }

        var deleteResult = await reviewRepository.RemoveAsync(review);

        if (!deleteResult.Success)
        {
            return Result.Fail(deleteResult.Error);
        }

        return Result.Ok();
    }

    public async Task<Result<double>> GetUserAverageRatingAsync(Guid userId)
    {
        var reviewsResult = await reviewRepository.GetListAsync<Review>(
            filter: r => r.ReviewedUserId == userId,
            includes: null,
            selector: null
        );

        if (!reviewsResult.Success)
        {
            return Result<double>.Fail(reviewsResult.Error);
        }

        var reviews = reviewsResult.Data.ToList();
        if (!reviews.Any())
        {
            return Result<double>.Ok(0);
        }

        var average = reviews.Average(r => r.Rating);
        return Result<double>.Ok(Math.Round(average, 2));
    }

    public async Task<Result<int>> GetUserReviewCountAsync(Guid userId)
    {
        var countResult = await reviewRepository.CountAsync(r => r.ReviewedUserId == userId);

        if (!countResult.Success)
        {
            return Result<int>.Fail(countResult.Error);
        }

        return Result<int>.Ok(countResult.Data);
    }

    private static ReviewResponse MapToResponse(Review review)
    {
        return new ReviewResponse
        {
            Id = review.Id,
            ExchangeId = review.ExchangeId,
            ReviewerId = review.ReviewerId,
            ReviewerFirstName = review.Reviewer?.FirstName ?? string.Empty,
            ReviewerLastName = review.Reviewer?.LastName ?? string.Empty,
            ReviewerImg = review.Reviewer?.Img,
            ReviewedUserId = review.ReviewedUserId,
            ReviewedUserFirstName = review.ReviewedUser?.FirstName ?? string.Empty,
            ReviewedUserLastName = review.ReviewedUser?.LastName ?? string.Empty,
            Rating = review.Rating,
            Comment = review.Comment,
            Created = review.Created,
            Modified = review.Modified
        };
    }
}

using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using DB.Models.Enums;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Realisations;

public class UserProfileService(
    IGenericRepository<User> userRepository,
    IGenericRepository<Book> bookRepository,
    IGenericRepository<Review> reviewRepository,
    IGenericRepository<Exchange> exchangeRepository)
    : IUserProfileService
{
    public async Task<Result<UserPublicProfileResponse>> GetUserPublicProfileAsync(Guid userId, Guid? requesterId = null)
    {
        var userResult = await userRepository.GetSingleAsync<User>(
            filter: u => u.Id == userId,
            includes: null,
            selector: null
        );

        if (!userResult.Success || userResult.Data == null)
        {
            return Result<UserPublicProfileResponse>.Fail("User not found");
        }

        var user = userResult.Data;

        var bookIncludes = new List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>
        {
            query => query.Include(b => b.Owner)
        };

        var booksResult = await bookRepository.GetListAsync<Book>(
            filter: b => b.OwnerId == userId,
            includes: bookIncludes,
            selector: null
        );

        var books = new List<BookResponse>();
        if (booksResult.Success && booksResult.Data != null)
        {
            books = booksResult.Data.Select(book => new BookResponse
            {
                Id = book.Id,
                OwnerId = book.OwnerId,
                OwnerFirstName = book.Owner?.FirstName ?? string.Empty,
                OwnerLastName = book.Owner?.LastName ?? string.Empty,
                Title = book.Title,
                Author = book.Author,
                Language = book.Language,
                Description = book.Description,
                State = book.State,
                Genre = book.Genre,
                Created = book.Created,
                Modified = book.Modified
            }).ToList();
        }

        var reviewIncludes = new List<Func<IQueryable<Review>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Review, object>>>
        {
            query => query.Include(r => r.Reviewer).Include(r => r.ReviewedUser)
        };

        var reviewsResult = await reviewRepository.GetListAsync<Review>(
            filter: r => r.ReviewedUserId == userId,
            includes: reviewIncludes,
            orderBy: q => q.OrderByDescending(r => r.Created),
            selector: null
        );

        var reviews = new List<ReviewResponse>();
        double averageRating = 0;
        int totalReviews = 0;

        if (reviewsResult.Success && reviewsResult.Data != null)
        {
            var reviewsList = reviewsResult.Data.ToList();
            totalReviews = reviewsList.Count;
            if (totalReviews > 0)
            {
                averageRating = Math.Round(reviewsList.Average(r => r.Rating), 2);
            }

            reviews = reviewsList.Select(r => new ReviewResponse
            {
                Id = r.Id,
                ExchangeId = r.ExchangeId,
                ReviewerId = r.ReviewerId,
                ReviewerFirstName = r.Reviewer?.FirstName ?? string.Empty,
                ReviewerLastName = r.Reviewer?.LastName ?? string.Empty,
                ReviewerImg = r.Reviewer?.Img,
                ReviewedUserId = r.ReviewedUserId,
                ReviewedUserFirstName = r.ReviewedUser?.FirstName ?? string.Empty,
                ReviewedUserLastName = r.ReviewedUser?.LastName ?? string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                Created = r.Created,
                Modified = r.Modified
            }).ToList();
        }

        var completedExchangesResult = await exchangeRepository.CountAsync(
            e => (e.OwnerId == userId || e.ReceiverId == userId) && e.Status == ExchangeStatus.Accepted
        );

        int completedExchanges = completedExchangesResult.Success ? completedExchangesResult.Data : 0;

        return Result<UserPublicProfileResponse>.Ok(new UserPublicProfileResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Location = user.Location,
            Img = user.Img,
            Description = user.Description,
            Created = user.Created,
            AverageRating = averageRating,
            TotalReviews = totalReviews,
            CompletedExchanges = completedExchanges,
            Books = books,
            Reviews = reviews
        });
    }

    public async Task<Result<IEnumerable<UserPublicProfileResponse>>> GetExchangeableUsersAsync(Guid currentUserId)
    {
        var exchangeIncludes = new List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>
        {
            query => query.Include(e => e.Owner).Include(e => e.Receiver)
        };

        var exchangesResult = await exchangeRepository.GetListAsync<Exchange>(
            filter: e => (e.OwnerId == currentUserId || e.ReceiverId == currentUserId) && e.Status == ExchangeStatus.Accepted,
            includes: exchangeIncludes,
            selector: null
        );

        if (!exchangesResult.Success)
        {
            return Result<IEnumerable<UserPublicProfileResponse>>.Fail(exchangesResult.Error);
        }

        var exchangePartnerIds = exchangesResult.Data
            .Select(e => e.OwnerId == currentUserId ? e.ReceiverId : e.OwnerId)
            .Distinct()
            .ToList();

        var profiles = new List<UserPublicProfileResponse>();

        foreach (var partnerId in exchangePartnerIds)
        {
            var profileResult = await GetUserPublicProfileAsync(partnerId, currentUserId);
            if (profileResult.Success)
            {
                profiles.Add(profileResult.Data);
            }
        }

        return Result<IEnumerable<UserPublicProfileResponse>>.Ok(profiles);
    }
}

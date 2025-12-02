using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using DB.Models.Enums;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Realisations;

public class BookSwipeService(
    IGenericRepository<BookSwipe> swipeRepository,
    IGenericRepository<Book> bookRepository,
    IGenericRepository<User> userRepository,
    INotificationService notificationService)
    : IBookSwipeService
{
    public async Task<Result<IEnumerable<SwipeableBookResponse>>> GetSwipeableBooksAsync(Guid userId, int limit = 10)
    {
        // Get all book IDs that user has already swiped
        var swipedBooksResult = await swipeRepository.GetListAsync<BookSwipe>(
            filter: s => s.UserId == userId,
            includes: null,
            selector: null
        );

        var swipedBookIds = new HashSet<Guid>();
        if (swipedBooksResult.Success && swipedBooksResult.Data != null)
        {
            swipedBookIds = swipedBooksResult.Data.Select(s => s.BookId).ToHashSet();
        }

        // Get books that user hasn't swiped yet and doesn't own
        var bookIncludes = new List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>
        {
            query => query.Include(b => b.Owner)
        };

        var booksResult = await bookRepository.GetListAsync<Book>(
            filter: b => b.OwnerId != userId && !swipedBookIds.Contains(b.Id),
            includes: bookIncludes,
            selector: null
        );

        if (!booksResult.Success)
        {
            return Result<IEnumerable<SwipeableBookResponse>>.Fail(booksResult.Error);
        }

        var swipeableBooks = booksResult.Data
            .OrderByDescending(b => b.Created)
            .Take(limit)
            .Select(book => new SwipeableBookResponse
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Description = book.Description,
                State = book.State,
                Genre = book.Genre,
                Language = book.Language ?? string.Empty,
                OwnerId = book.OwnerId,
                OwnerFirstName = book.Owner?.FirstName ?? string.Empty,
                OwnerLastName = book.Owner?.LastName ?? string.Empty,
                OwnerLocation = book.Owner?.Location,
                OwnerImg = book.Owner?.Img,
                Created = book.Created
            });

        return Result<IEnumerable<SwipeableBookResponse>>.Ok(swipeableBooks);
    }

    public async Task<Result<SwipeResponse>> SwipeBookAsync(Guid userId, SwipeRequest request)
    {
        // Check if user has already swiped this book
        var existingSwipeResult = await swipeRepository.GetSingleAsync<BookSwipe>(
            filter: s => s.UserId == userId && s.BookId == request.BookId,
            includes: null,
            selector: null
        );

        if (existingSwipeResult.Success && existingSwipeResult.Data != null)
        {
            return Result<SwipeResponse>.Fail("You have already swiped this book");
        }

        // Get the book to validate it exists and get owner info
        var bookIncludes = new List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>
        {
            query => query.Include(b => b.Owner)
        };

        var bookResult = await bookRepository.GetSingleAsync<Book>(
            filter: b => b.Id == request.BookId,
            includes: bookIncludes,
            selector: null
        );

        if (!bookResult.Success || bookResult.Data == null)
        {
            return Result<SwipeResponse>.Fail("Book not found");
        }

        var book = bookResult.Data;

        // Can't swipe own book
        if (book.OwnerId == userId)
        {
            return Result<SwipeResponse>.Fail("You cannot swipe your own book");
        }

        // Create the swipe
        var swipe = new BookSwipe
        {
            UserId = userId,
            BookId = request.BookId,
            SwipeType = request.SwipeType
        };

        var addResult = await swipeRepository.AddAsync(swipe);

        if (!addResult.Success)
        {
            return Result<SwipeResponse>.Fail(addResult.Error);
        }

        // If it's a like, notify the book owner
        if (request.SwipeType == SwipeType.Like)
        {
            var userResult = await userRepository.GetSingleAsync<User>(
                filter: u => u.Id == userId,
                includes: null,
                selector: null
            );

            if (userResult.Success && userResult.Data != null)
            {
                var liker = userResult.Data;
                await notificationService.NotifyBookLikeAsync(book.OwnerId, new BookLikeNotification
                {
                    Message = $"{liker.FirstName} {liker.LastName} liked your book \"{book.Title}\"",
                    BookId = book.Id,
                    BookTitle = book.Title,
                    LikedByUserId = userId,
                    LikedByUserFirstName = liker.FirstName,
                    LikedByUserLastName = liker.LastName
                });
            }
        }

        var response = new SwipeResponse
        {
            Id = swipe.Id,
            UserId = userId,
            BookId = request.BookId,
            BookTitle = book.Title,
            SwipeType = request.SwipeType,
            Created = swipe.Created
        };

        return Result<SwipeResponse>.Ok(response);
    }

    public async Task<Result<IEnumerable<SwipeResponse>>> GetUserLikesAsync(Guid userId)
    {
        var includes = new List<Func<IQueryable<BookSwipe>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<BookSwipe, object>>>
        {
            query => query.Include(s => s.Book)
        };

        var likesResult = await swipeRepository.GetListAsync<BookSwipe>(
            filter: s => s.UserId == userId && s.SwipeType == SwipeType.Like,
            includes: includes,
            selector: null
        );

        if (!likesResult.Success)
        {
            return Result<IEnumerable<SwipeResponse>>.Fail(likesResult.Error);
        }

        var likes = likesResult.Data.Select(swipe => new SwipeResponse
        {
            Id = swipe.Id,
            UserId = swipe.UserId,
            BookId = swipe.BookId,
            BookTitle = swipe.Book?.Title ?? string.Empty,
            SwipeType = swipe.SwipeType,
            Created = swipe.Created
        });

        return Result<IEnumerable<SwipeResponse>>.Ok(likes);
    }
}

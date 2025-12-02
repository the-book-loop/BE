using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using API.Services.Realisations.Utilites;
using DB.Models.Enums;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace API.Services.Realisations;

public class ExchangeService(
    IGenericRepository<Exchange> exchangeRepository,
    IGenericRepository<Book> bookRepository,
    IGenericRepository<User> userRepository,
    INotificationService notificationService)
    : IExchangeService
{
    public async Task<Result<IEnumerable<ExchangeResponse>>> GetExchangesAsync(ExchangeFilterRequest request = null)
    {
        var includes = new List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>
        {
            query => query
                .Include(e => e.Book)
                .Include(e => e.Owner)
                .Include(e => e.Receiver)
        };

        Expression<Func<Exchange, bool>> filter = e => true;

        if (request is not null)
        {
            if (request.UserId.HasValue)
                filter = filter.And(e => e.OwnerId == request.UserId.Value || e.ReceiverId == request.UserId.Value);
            else if (request.ReceiverId.HasValue)
                filter = filter.And(e => e.ReceiverId == request.ReceiverId.Value);
            else if (request.OwnerId.HasValue)
                filter = filter.And(e => e.OwnerId == request.OwnerId.Value);
            if (request.BookId.HasValue)
                filter = filter.And(e => e.BookId == request.BookId.Value);
            if (request.status.HasValue)
                filter = filter.And(e => e.Status == request.status.Value);
            if (request.CreatedFrom.HasValue)
                filter = filter.And(e => e.Created >= request.CreatedFrom.Value);
            if (request.CreatedTo.HasValue)
                filter = filter.And(e => e.Created <= request.CreatedTo.Value);
        }

        var exchangesResult = await exchangeRepository.GetListAsync<Exchange>(
            filter: filter,
            includes: includes,
            selector: null
        );

        if (!exchangesResult.Success)
        {
            return Result<IEnumerable<ExchangeResponse>>.Fail(exchangesResult.Error);
        }
        var exchangeResponses = exchangesResult.Data.Select(exchange => new ExchangeResponse
        {
            Id = exchange.Id,
            BookId = exchange.Book.Id,
            BookTitle = exchange.Book.Title,
            OwnerId = exchange.OwnerId,
            OwnerFirstName = exchange.Owner?.FirstName ?? string.Empty,
            OwnerLastName = exchange.Owner?.LastName ?? string.Empty,
            ReceiverId = exchange.ReceiverId,
            ReceiverFirstName = exchange.Receiver?.FirstName ?? string.Empty,
            ReceiverLastName = exchange.Receiver?.LastName ?? string.Empty,
            Status = exchange.Status,
            Rating = exchange.Rating,
            Comment = exchange.Comment ?? string.Empty,
            Created = exchange.Created,
            Modified = exchange.Modified
        });

        return Result<IEnumerable<ExchangeResponse>>.Ok(exchangeResponses);
    }

    public async Task<Result<ExchangeResponse>> GetExchangeByIdAsync(Guid exchangeId)
    {
        var includes = new List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>
        {
            query => query
                .Include(e => e.Book)
                .Include(e => e.Owner)
                .Include(e => e.Receiver)
        };
        var exchangeResult = await exchangeRepository.GetSingleAsync<Exchange>(
            filter: e => e.Id == exchangeId,
            includes: includes,
            selector: null
        );

        if (!exchangeResult.Success || exchangeResult.Data == null)
        {
            return Result<ExchangeResponse>.Fail("Exchange not found");
        }
        var exchange = exchangeResult.Data;
        var exchangeResponse = new ExchangeResponse
        {
            Id = exchange.Id,
            BookId = exchange.Book.Id,
            BookTitle = exchange.Book.Title,
            OwnerId = exchange.OwnerId,
            OwnerFirstName = exchange.Owner?.FirstName ?? string.Empty,
            OwnerLastName = exchange.Owner?.LastName ?? string.Empty,
            ReceiverId = exchange.ReceiverId,
            ReceiverFirstName = exchange.Receiver?.FirstName ?? string.Empty,
            ReceiverLastName = exchange.Receiver?.LastName ?? string.Empty,
            Status = exchange.Status,
            Rating = exchange.Rating,
            Comment = exchange.Comment ?? string.Empty,
            Created = exchange.Created,
            Modified = exchange.Modified
        };

        return Result<ExchangeResponse>.Ok(exchangeResponse);
    }

    public async Task<Result<ExchangeDetailsResponse>> GetExchangeDetailsAsync(Guid exchangeId, Guid userId)
    {
        var includes = new List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>
        {
            query => query
                .Include(e => e.Book)
                .Include(e => e.Owner)
                .Include(e => e.Receiver)
        };

        var exchangeResult = await exchangeRepository.GetSingleAsync<Exchange>(
            filter: e => e.Id == exchangeId,
            includes: includes,
            selector: null
        );

        if (!exchangeResult.Success || exchangeResult.Data == null)
        {
            return Result<ExchangeDetailsResponse>.Fail("Exchange not found");
        }

        var exchange = exchangeResult.Data;

        if (exchange.OwnerId != userId && exchange.ReceiverId != userId)
        {
            return Result<ExchangeDetailsResponse>.Fail("You are not authorized to view this exchange details");
        }

        var exchangeResponse = new ExchangeResponse
        {
            Id = exchange.Id,
            BookId = exchange.Book.Id,
            BookTitle = exchange.Book.Title,
            OwnerId = exchange.OwnerId,
            OwnerFirstName = exchange.Owner?.FirstName ?? string.Empty,
            OwnerLastName = exchange.Owner?.LastName ?? string.Empty,
            ReceiverId = exchange.ReceiverId,
            ReceiverFirstName = exchange.Receiver?.FirstName ?? string.Empty,
            ReceiverLastName = exchange.Receiver?.LastName ?? string.Empty,
            Status = exchange.Status,
            Rating = exchange.Rating,
            Comment = exchange.Comment ?? string.Empty,
            Created = exchange.Created,
            Modified = exchange.Modified
        };

        // Determine requester (the person who initiated the exchange request - the receiver)
        var requesterId = exchange.ReceiverId;

        // Get requester's profile
        var userResult = await userRepository.GetSingleAsync<User>(
            filter: u => u.Id == requesterId,
            includes: null,
            selector: null
        );

        if (!userResult.Success || userResult.Data == null)
        {
            return Result<ExchangeDetailsResponse>.Fail("Requester not found");
        }

        var requester = userResult.Data;
        var requesterProfile = new UserProfileResponse
        {
            Id = requester.Id,
            FirstName = requester.FirstName,
            LastName = requester.LastName,
            Location = requester.Location,
            Img = requester.Img,
            Description = requester.Description,
            Created = requester.Created
        };

        // Get requester's books
        var bookIncludes = new List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>
        {
            query => query.Include(b => b.Owner).Include(b => b.Language)
        };

        var booksResult = await bookRepository.GetListAsync<Book>(
            filter: b => b.OwnerId == requesterId,
            includes: bookIncludes,
            selector: null
        );

        var requesterBooks = new List<BookResponse>();
        if (booksResult.Success && booksResult.Data != null)
        {
            requesterBooks = booksResult.Data.Select(book => new BookResponse
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

        return Result<ExchangeDetailsResponse>.Ok(new ExchangeDetailsResponse
        {
            Exchange = exchangeResponse,
            RequesterProfile = requesterProfile,
            RequesterBooks = requesterBooks
        });
    }

    public async Task<Result<ExchangeResponse>> AddExchangeAsync(Guid receiverId, AddExchangeRequest request)
    {
        if (receiverId == request.OwnerId)
        {
            return Result<ExchangeResponse>.Fail("You cannot create exchange yourself");
        }

        var exchange = new Exchange
        {
            BookId = request.BookId,
            OwnerId = request.OwnerId,
            ReceiverId = receiverId,
            Status = ExchangeStatus.Pending,
            Comment = string.Empty
        };

        var addResult = await exchangeRepository.AddAsync(exchange);

        if (!addResult.Success)
        {
            return Result<ExchangeResponse>.Fail(addResult.Error);
        }

        var addedExchange = await GetExchangeByIdAsync(exchange.Id);

        await notificationService.NotifyUserAsync(request.OwnerId, new ExchangeNotification
        {
            Message = "You have exchange proposal",
            Data = addedExchange.Data
        });
        return addedExchange;
    }

    public async Task<Result<ExchangeResponse>> UpdateExchangeAsync(Guid exchangeId, Guid userId, UpdateExchangeRequest request)
    {
        var exchangeResult = await exchangeRepository.GetSingleAsync<Exchange>(
            e => e.Id == exchangeId,
            includes: null
        );

        if (!exchangeResult.Success || exchangeResult.Data == null)
        {
            return Result<ExchangeResponse>.Fail("Exchange not found");
        }

        var exchange = exchangeResult.Data;

        if (exchange.OwnerId == userId) return await UpdateAsOwnerAsync(exchange, request);

        if (exchange.ReceiverId == userId) return await UpdateAsReceiverAsync(exchange, request);

        return Result<ExchangeResponse>.Fail("You are not authorized to update this exchange");
    }


    private async Task<Result<ExchangeResponse>> UpdateAsOwnerAsync(Exchange exchange, UpdateExchangeRequest request)
    {
        if (request.Comment != null || request.Rating.HasValue)
        {
            return Result<ExchangeResponse>.Fail("Owner cannot change rating or comment");
        }

        var previousStatus = exchange.Status;
        exchange.Status = request.Status ?? exchange.Status;
        exchange.Modified = DateTime.UtcNow;

        var updateResult = await exchangeRepository.UpdateAsync(exchange);
        if (!updateResult.Success)
        {
            return Result<ExchangeResponse>.Fail(updateResult.Error);
        }

        var updatedExchange = await GetExchangeByIdAsync(exchange.Id);

        // Determine notification message based on status change
        string receiverMessage;
        string ownerMessage;

        if (request.Status == ExchangeStatus.Accepted)
        {
            receiverMessage = $"Your exchange request for \"{updatedExchange.Data.BookTitle}\" has been accepted";
            ownerMessage = $"You have accepted the exchange request for \"{updatedExchange.Data.BookTitle}\"";
        }
        else if (request.Status == ExchangeStatus.Declined)
        {
            receiverMessage = $"Your exchange request for \"{updatedExchange.Data.BookTitle}\" has been declined";
            ownerMessage = $"You have declined the exchange request for \"{updatedExchange.Data.BookTitle}\"";
        }
        else
        {
            receiverMessage = "Book owner has updated exchange status";
            ownerMessage = "Exchange status has been updated";
        }

        // Notify the receiver (requester)
        await notificationService.NotifyUserAsync(exchange.ReceiverId, new ExchangeNotification
        {
            Message = receiverMessage,
            Data = updatedExchange.Data
        });

        // Notify the owner (confirmation)
        await notificationService.NotifyUserAsync(exchange.OwnerId, new ExchangeNotification
        {
            Message = ownerMessage,
            Data = updatedExchange.Data
        });

        return updatedExchange;
    }

    private async Task<Result<ExchangeResponse>> UpdateAsReceiverAsync(Exchange exchange, UpdateExchangeRequest request)
    {
        if (request.Status.HasValue && request.Status != exchange.Status)
        {
            return Result<ExchangeResponse>.Fail("Receiver cannot change status");
        }

        if (exchange.Status == ExchangeStatus.Pending)
        {
            return Result<ExchangeResponse>.Fail("You cannot rate pending exchange");
        }

        exchange.Comment = request.Comment ?? exchange.Comment;
        exchange.Rating = request.Rating ?? exchange.Rating;
        exchange.Modified = DateTime.UtcNow;

        var updateResult = await exchangeRepository.UpdateAsync(exchange);
        
        if (!updateResult.Success)
        {
            return Result<ExchangeResponse>.Fail(updateResult.Error);
        }

        var addedExchange = await GetExchangeByIdAsync(exchange.Id);

        await notificationService.NotifyUserAsync(exchange.OwnerId, new ExchangeNotification
        {
            Message = "Receiver left a review",
            Data = addedExchange.Data
        });

        return addedExchange;
    }


    public async Task<Result> DeleteExchangeAsync(Guid exchangeId, Guid userId)
    {
        var exchangeResult = await exchangeRepository.GetSingleAsync<Exchange>(
            filter: e => e.Id == exchangeId,
            includes: null,
            selector: null
        );

        if (!exchangeResult.Success || exchangeResult.Data == null)
        {
            return Result.Fail("Exchange not found");
        }

        var exchange = exchangeResult.Data;

        if (exchange.ReceiverId != userId)
        {
            return Result.Fail("You are not authorized to delete this book");
        }
        else if (exchange.Status != ExchangeStatus.Pending)
        {
            return Result.Fail("You are not allowed to cancel exchange on this stage");
        }
        
        var deleteResult = await exchangeRepository.RemoveAsync(exchange);

        if (!deleteResult.Success)
        {
            return Result.Fail(deleteResult.Error);
        }

        return Result.Ok();
    }
}
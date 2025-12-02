using API.Services.Interfaces;
using API.Services.Realisations.Utilites;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace API.Services.Realisations;

public class BookService(IGenericRepository<Book> bookRepository) : IBookService
{
    public async Task<Result<IEnumerable<BookResponse>>> GetBooksAsync(BookFilterRequest request = null)
    {
        var includes = new List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>
        {
            query => query.Include(b => b.Owner)
        };

        Expression<Func<Book, bool>> filter = b => true;

        if (request is not null)
        {
            if (!string.IsNullOrEmpty(request.Title))
                filter = filter.And(b => b.Title.ToLower().Contains(request.Title.ToLower()));
            if (!string.IsNullOrEmpty(request.Author))
                filter = filter.And(b => b.Author.ToLower().Contains(request.Author.ToLower()));
            if (!string.IsNullOrEmpty(request.Language))
                filter = filter.And(b => b.Language.ToLower().Contains(request.Language.ToLower()));
            if (!string.IsNullOrEmpty(request.Description))
                filter = filter.And(b => b.Description.ToLower().Contains(request.Description.ToLower()));
            if (!string.IsNullOrEmpty(request.State))
                filter = filter.And(b => b.State.ToLower().Contains(request.State.ToLower()));
            if (!string.IsNullOrEmpty(request.Genre))
                filter = filter.And(b => b.Genre.ToLower().Contains(request.Genre.ToLower()));
            if (request.OwnerId.HasValue)
                filter = filter.And(b => b.OwnerId == request.OwnerId.Value);
        }

        var booksResult = await bookRepository.GetListAsync<Book>(
            filter: filter,
            includes: includes,
            selector: null
        );

        if (!booksResult.Success)
        {
            return Result<IEnumerable<BookResponse>>.Fail(booksResult.Error);
        }
        var bookResponses = booksResult.Data.Select(book => new BookResponse
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
        });

        return Result<IEnumerable<BookResponse>>.Ok(bookResponses);
    }

    public async Task<Result<BookResponse>> GetBookByIdAsync(Guid bookId)
    {
        var includes = new List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>
        {
            query => query.Include(b => b.Owner)
        };
        var bookResult = await bookRepository.GetSingleAsync<Book>(
            filter: b => b.Id == bookId,
            includes: includes,
            selector: null
        );

        if (!bookResult.Success || bookResult.Data == null)
        {
            return Result<BookResponse>.Fail("Book not found");
        }
        var book = bookResult.Data;
        var bookResponse = new BookResponse
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
        };

        return Result<BookResponse>.Ok(bookResponse);
    }

    public async Task<Result<BookResponse>> AddBookAsync(Guid ownerId, AddBookRequest request)
    {
        var book = new Book
        {
            OwnerId = ownerId,
            Title = request.Title,
            Author = request.Author,
            Language = request.Language,
            Description = request.Description,
            State = request.State,
            Genre = request.Genre
        };

        var addResult = await bookRepository.AddAsync(book);

        if (!addResult.Success)
        {
            return Result<BookResponse>.Fail(addResult.Error);
        }

        return await GetBookByIdAsync(book.Id);
    }

    public async Task<Result<BookResponse>> UpdateBookAsync(Guid bookId, Guid userId, UpdateBookRequest request)
    {
        var bookResult = await bookRepository.GetSingleAsync<Book>(
            filter: b => b.Id == bookId,
            includes: null,
            selector: null
        );

        if (!bookResult.Success || bookResult.Data == null)
        {
            return Result<BookResponse>.Fail("Book not found");
        }

        var book = bookResult.Data;

        if (book.OwnerId != userId)
        {
            return Result<BookResponse>.Fail("You are not authorized to update this book");
        }

        book.Title = request.Title ?? book.Title;
        book.Author = request.Author ?? book.Author;
        book.Language = request.Language ?? book.Language;
        book.Description = request.Description ?? book.Description;
        book.State = request.State ?? book.State;
        book.Genre = request.Genre ?? book.Genre;
        book.Modified = DateTime.UtcNow;

        var updateResult = await bookRepository.UpdateAsync(book);

        if (!updateResult.Success)
        {
            return Result<BookResponse>.Fail(updateResult.Error);
        }

        return await GetBookByIdAsync(bookId);
    }

    public async Task<Result> DeleteBookAsync(Guid bookId, Guid userId)
    {
        var bookResult = await bookRepository.GetSingleAsync<Book>(
            filter: b => b.Id == bookId,
            includes: null,
            selector: null
        );

        if (!bookResult.Success || bookResult.Data == null)
        {
            return Result.Fail("Book not found");
        }

        var book = bookResult.Data;

        if (book.OwnerId != userId)
        {
            return Result.Fail("You are not authorized to delete this book");
        }

        var deleteResult = await bookRepository.RemoveAsync(book);

        if (!deleteResult.Success)
        {
            return Result.Fail(deleteResult.Error);
        }

        return Result.Ok();
    }
}

using API.Services.Realisations;
using DB.DTOs;
using DB.Models;
using DB.Models.Enums;
using DB.Repository;
using DB.Repository.Utilites;
using Moq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace TEST.Services;

[TestFixture]
public class UserProfileServiceTests
{
    private Mock<IGenericRepository<User>> _userRepositoryMock;
    private Mock<IGenericRepository<Book>> _bookRepositoryMock;
    private Mock<IGenericRepository<Review>> _reviewRepositoryMock;
    private Mock<IGenericRepository<Exchange>> _exchangeRepositoryMock;
    private UserProfileService _userProfileService;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IGenericRepository<User>>();
        _bookRepositoryMock = new Mock<IGenericRepository<Book>>();
        _reviewRepositoryMock = new Mock<IGenericRepository<Review>>();
        _exchangeRepositoryMock = new Mock<IGenericRepository<Exchange>>();
        _userProfileService = new UserProfileService(
            _userRepositoryMock.Object,
            _bookRepositoryMock.Object,
            _reviewRepositoryMock.Object,
            _exchangeRepositoryMock.Object);
    }

    #region GetUserPublicProfileAsync Tests

    [Test]
    public async Task GetUserPublicProfileAsync_WithValidUserId_ReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Location = "New York",
            Img = "profile.jpg",
            Description = "Book lover",
            Created = DateTime.UtcNow
        };

        var books = new List<Book>
        {
            new Book
            {
                Id = Guid.NewGuid(),
                OwnerId = userId,
                Owner = user,
                Title = "Test Book",
                Author = "Test Author",
                Language = "English",
                Description = "A test book",
                State = "Good",
                Genre = "Fiction",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }
        };

        var reviewerId = Guid.NewGuid();
        var reviews = new List<Review>
        {
            new Review
            {
                Id = Guid.NewGuid(),
                ExchangeId = Guid.NewGuid(),
                ReviewerId = reviewerId,
                Reviewer = new User { Id = reviewerId, FirstName = "Jane", LastName = "Smith" },
                ReviewedUserId = userId,
                ReviewedUser = user,
                Rating = 5,
                Comment = "Great trader!",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<User>.Ok(user));

        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(books));

        _reviewRepositoryMock
            .Setup(x => x.GetListAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                It.IsAny<Func<IQueryable<Review>, IOrderedQueryable<Review>>>(),
                It.IsAny<List<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Review>>.Ok(reviews));

        _exchangeRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<Expression<Func<Exchange, bool>>>(), default))
            .ReturnsAsync(Result<int>.Ok(3));

        // Act
        var result = await _userProfileService.GetUserPublicProfileAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Id, Is.EqualTo(userId));
        Assert.That(result.Data.FirstName, Is.EqualTo("John"));
        Assert.That(result.Data.LastName, Is.EqualTo("Doe"));
        Assert.That(result.Data.AverageRating, Is.EqualTo(5.0));
        Assert.That(result.Data.TotalReviews, Is.EqualTo(1));
        Assert.That(result.Data.CompletedExchanges, Is.EqualTo(3));
        Assert.That(result.Data.Books.Count(), Is.EqualTo(1));
        Assert.That(result.Data.Reviews.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetUserPublicProfileAsync_WithNoReviews_ReturnsZeroRating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Created = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<User>.Ok(user));

        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(new List<Book>()));

        _reviewRepositoryMock
            .Setup(x => x.GetListAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                It.IsAny<Func<IQueryable<Review>, IOrderedQueryable<Review>>>(),
                It.IsAny<List<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Review>>.Ok(new List<Review>()));

        _exchangeRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<Expression<Func<Exchange, bool>>>(), default))
            .ReturnsAsync(Result<int>.Ok(0));

        // Act
        var result = await _userProfileService.GetUserPublicProfileAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.AverageRating, Is.EqualTo(0));
        Assert.That(result.Data.TotalReviews, Is.EqualTo(0));
    }

    [Test]
    public async Task GetUserPublicProfileAsync_WithInvalidUserId_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<User>.Ok(null!));

        // Act
        var result = await _userProfileService.GetUserPublicProfileAsync(userId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("User not found"));
    }

    [Test]
    public async Task GetUserPublicProfileAsync_CalculatesAverageRatingCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Created = DateTime.UtcNow
        };

        var reviews = new List<Review>
        {
            new Review
            {
                Id = Guid.NewGuid(),
                ReviewerId = Guid.NewGuid(),
                Reviewer = new User { Id = Guid.NewGuid(), FirstName = "R1", LastName = "U1" },
                ReviewedUserId = userId,
                ReviewedUser = user,
                Rating = 5,
                Created = DateTime.UtcNow
            },
            new Review
            {
                Id = Guid.NewGuid(),
                ReviewerId = Guid.NewGuid(),
                Reviewer = new User { Id = Guid.NewGuid(), FirstName = "R2", LastName = "U2" },
                ReviewedUserId = userId,
                ReviewedUser = user,
                Rating = 4,
                Created = DateTime.UtcNow
            },
            new Review
            {
                Id = Guid.NewGuid(),
                ReviewerId = Guid.NewGuid(),
                Reviewer = new User { Id = Guid.NewGuid(), FirstName = "R3", LastName = "U3" },
                ReviewedUserId = userId,
                ReviewedUser = user,
                Rating = 3,
                Created = DateTime.UtcNow
            }
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<User>.Ok(user));

        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(new List<Book>()));

        _reviewRepositoryMock
            .Setup(x => x.GetListAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                It.IsAny<Func<IQueryable<Review>, IOrderedQueryable<Review>>>(),
                It.IsAny<List<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Review>>.Ok(reviews));

        _exchangeRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<Expression<Func<Exchange, bool>>>(), default))
            .ReturnsAsync(Result<int>.Ok(0));

        // Act
        var result = await _userProfileService.GetUserPublicProfileAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.AverageRating, Is.EqualTo(4.0));
        Assert.That(result.Data.TotalReviews, Is.EqualTo(3));
    }

    #endregion

    #region GetExchangeableUsersAsync Tests

    [Test]
    public async Task GetExchangeableUsersAsync_WithAcceptedExchanges_ReturnsPartnerProfiles()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        var partner = new User
        {
            Id = partnerId,
            FirstName = "Partner",
            LastName = "User",
            Created = DateTime.UtcNow
        };

        var exchanges = new List<Exchange>
        {
            new Exchange
            {
                Id = Guid.NewGuid(),
                OwnerId = currentUserId,
                Owner = new User { Id = currentUserId, FirstName = "Current", LastName = "User" },
                ReceiverId = partnerId,
                Receiver = partner,
                Status = ExchangeStatus.Accepted
            }
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetListAsync<Exchange>(
                It.IsAny<Expression<Func<Exchange, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Exchange>, IIncludableQueryable<Exchange, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Exchange>>.Ok(exchanges));

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<User>.Ok(partner));

        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(new List<Book>()));

        _reviewRepositoryMock
            .Setup(x => x.GetListAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                It.IsAny<Func<IQueryable<Review>, IOrderedQueryable<Review>>>(),
                It.IsAny<List<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Review>>.Ok(new List<Review>()));

        _exchangeRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<Expression<Func<Exchange, bool>>>(), default))
            .ReturnsAsync(Result<int>.Ok(1));

        // Act
        var result = await _userProfileService.GetExchangeableUsersAsync(currentUserId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Count(), Is.EqualTo(1));
        Assert.That(result.Data.First().Id, Is.EqualTo(partnerId));
    }

    [Test]
    public async Task GetExchangeableUsersAsync_WithNoExchanges_ReturnsEmptyList()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();

        _exchangeRepositoryMock
            .Setup(x => x.GetListAsync<Exchange>(
                It.IsAny<Expression<Func<Exchange, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Exchange>, IIncludableQueryable<Exchange, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Exchange>>.Ok(new List<Exchange>()));

        // Act
        var result = await _userProfileService.GetExchangeableUsersAsync(currentUserId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetExchangeableUsersAsync_WithMultipleExchangesWithSamePartner_ReturnsDistinctProfiles()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();

        var partner = new User
        {
            Id = partnerId,
            FirstName = "Partner",
            LastName = "User",
            Created = DateTime.UtcNow
        };

        var exchanges = new List<Exchange>
        {
            new Exchange
            {
                Id = Guid.NewGuid(),
                OwnerId = currentUserId,
                Owner = new User { Id = currentUserId, FirstName = "Current", LastName = "User" },
                ReceiverId = partnerId,
                Receiver = partner,
                Status = ExchangeStatus.Accepted
            },
            new Exchange
            {
                Id = Guid.NewGuid(),
                OwnerId = partnerId,
                Owner = partner,
                ReceiverId = currentUserId,
                Receiver = new User { Id = currentUserId, FirstName = "Current", LastName = "User" },
                Status = ExchangeStatus.Accepted
            }
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetListAsync<Exchange>(
                It.IsAny<Expression<Func<Exchange, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Exchange>, IIncludableQueryable<Exchange, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Exchange>>.Ok(exchanges));

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<User>.Ok(partner));

        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(new List<Book>()));

        _reviewRepositoryMock
            .Setup(x => x.GetListAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                It.IsAny<Func<IQueryable<Review>, IOrderedQueryable<Review>>>(),
                It.IsAny<List<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Review>>.Ok(new List<Review>()));

        _exchangeRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<Expression<Func<Exchange, bool>>>(), default))
            .ReturnsAsync(Result<int>.Ok(2));

        // Act
        var result = await _userProfileService.GetExchangeableUsersAsync(currentUserId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetExchangeableUsersAsync_WhenRepositoryFails_ReturnsFailure()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();

        _exchangeRepositoryMock
            .Setup(x => x.GetListAsync<Exchange>(
                It.IsAny<Expression<Func<Exchange, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Exchange>, IIncludableQueryable<Exchange, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Exchange>>.Fail("Database error"));

        // Act
        var result = await _userProfileService.GetExchangeableUsersAsync(currentUserId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
    }

    #endregion
}

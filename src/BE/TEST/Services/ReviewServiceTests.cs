using API.Services.Interfaces;
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
public class ReviewServiceTests
{
    private Mock<IGenericRepository<Review>> _reviewRepositoryMock;
    private Mock<IGenericRepository<Exchange>> _exchangeRepositoryMock;
    private Mock<INotificationService> _notificationServiceMock;
    private ReviewService _reviewService;

    [SetUp]
    public void SetUp()
    {
        _reviewRepositoryMock = new Mock<IGenericRepository<Review>>();
        _exchangeRepositoryMock = new Mock<IGenericRepository<Exchange>>();
        _notificationServiceMock = new Mock<INotificationService>();
        _reviewService = new ReviewService(
            _reviewRepositoryMock.Object,
            _exchangeRepositoryMock.Object,
            _notificationServiceMock.Object);
    }

    #region GetReviewsAsync Tests

    [Test]
    public async Task GetReviewsAsync_WithNoFilter_ReturnsAllReviews()
    {
        // Arrange
        var reviewerId = Guid.NewGuid();
        var reviewedUserId = Guid.NewGuid();
        var reviews = new List<Review>
        {
            new Review
            {
                Id = Guid.NewGuid(),
                ExchangeId = Guid.NewGuid(),
                ReviewerId = reviewerId,
                Reviewer = new User { Id = reviewerId, FirstName = "John", LastName = "Doe" },
                ReviewedUserId = reviewedUserId,
                ReviewedUser = new User { Id = reviewedUserId, FirstName = "Jane", LastName = "Smith" },
                Rating = 5,
                Comment = "Great exchange!",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            },
            new Review
            {
                Id = Guid.NewGuid(),
                ExchangeId = Guid.NewGuid(),
                ReviewerId = reviewedUserId,
                Reviewer = new User { Id = reviewedUserId, FirstName = "Jane", LastName = "Smith" },
                ReviewedUserId = reviewerId,
                ReviewedUser = new User { Id = reviewerId, FirstName = "John", LastName = "Doe" },
                Rating = 4,
                Comment = "Good experience",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }
        };

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

        // Act
        var result = await _reviewService.GetReviewsAsync();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetReviewsAsync_WithReviewedUserIdFilter_ReturnsFilteredReviews()
    {
        // Arrange
        var reviewedUserId = Guid.NewGuid();
        var reviews = new List<Review>
        {
            new Review
            {
                Id = Guid.NewGuid(),
                ExchangeId = Guid.NewGuid(),
                ReviewerId = Guid.NewGuid(),
                Reviewer = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" },
                ReviewedUserId = reviewedUserId,
                ReviewedUser = new User { Id = reviewedUserId, FirstName = "Jane", LastName = "Smith" },
                Rating = 5,
                Comment = "Great!",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }
        };

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

        var filter = new ReviewFilterRequest { ReviewedUserId = reviewedUserId };

        // Act
        var result = await _reviewService.GetReviewsAsync(filter);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Count(), Is.EqualTo(1));
        Assert.That(result.Data.First().ReviewedUserId, Is.EqualTo(reviewedUserId));
    }

    [Test]
    public async Task GetReviewsAsync_WhenRepositoryFails_ReturnsFailure()
    {
        // Arrange
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
            .ReturnsAsync(Result<IEnumerable<Review>>.Fail("Database error"));

        // Act
        var result = await _reviewService.GetReviewsAsync();

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
    }

    #endregion

    #region GetReviewByIdAsync Tests

    [Test]
    public async Task GetReviewByIdAsync_WithValidId_ReturnsReview()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var reviewedUserId = Guid.NewGuid();

        var review = new Review
        {
            Id = reviewId,
            ExchangeId = Guid.NewGuid(),
            ReviewerId = reviewerId,
            Reviewer = new User { Id = reviewerId, FirstName = "John", LastName = "Doe" },
            ReviewedUserId = reviewedUserId,
            ReviewedUser = new User { Id = reviewedUserId, FirstName = "Jane", LastName = "Smith" },
            Rating = 5,
            Comment = "Excellent!",
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _reviewRepositoryMock
            .Setup(x => x.GetSingleAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                It.IsAny<List<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Review>.Ok(review));

        // Act
        var result = await _reviewService.GetReviewByIdAsync(reviewId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Id, Is.EqualTo(reviewId));
        Assert.That(result.Data.Rating, Is.EqualTo(5));
    }

    [Test]
    public async Task GetReviewByIdAsync_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _reviewRepositoryMock
            .Setup(x => x.GetSingleAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                It.IsAny<List<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Review>.Ok(null!));

        // Act
        var result = await _reviewService.GetReviewByIdAsync(reviewId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Review not found"));
    }

    #endregion

    #region AddReviewAsync Tests

    [Test]
    public async Task AddReviewAsync_WithValidRequest_CreatesReview()
    {
        // Arrange
        var exchangeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var reviewedUserId = Guid.NewGuid();

        var exchange = new Exchange
        {
            Id = exchangeId,
            OwnerId = reviewedUserId,
            Owner = new User { Id = reviewedUserId, FirstName = "Owner", LastName = "User" },
            ReceiverId = reviewerId,
            Receiver = new User { Id = reviewerId, FirstName = "Receiver", LastName = "User" },
            Status = ExchangeStatus.Accepted
        };

        var request = new AddReviewRequest
        {
            ExchangeId = exchangeId,
            Rating = 5,
            Comment = "Great exchange!"
        };

        var createdReview = new Review
        {
            Id = Guid.NewGuid(),
            ExchangeId = exchangeId,
            ReviewerId = reviewerId,
            Reviewer = new User { Id = reviewerId, FirstName = "Receiver", LastName = "User" },
            ReviewedUserId = reviewedUserId,
            ReviewedUser = new User { Id = reviewedUserId, FirstName = "Owner", LastName = "User" },
            Rating = 5,
            Comment = "Great exchange!",
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(exchange));

        // Setup sequence for checking existing review (null) then getting created review
        var callCount = 0;
        _reviewRepositoryMock
            .Setup(x => x.GetSingleAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                It.IsAny<List<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    return Result<Review>.Ok(null!); // First call: check for existing review
                return Result<Review>.Ok(createdReview); // Second call: get created review
            });

        _reviewRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Review>(), default))
            .ReturnsAsync(Result.Ok());

        _notificationServiceMock
            .Setup(x => x.NotifyReviewAsync(It.IsAny<Guid>(), It.IsAny<ReviewNotification>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _reviewService.AddReviewAsync(reviewerId, request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Rating, Is.EqualTo(5));
        _reviewRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Review>(), default), Times.Once);
    }

    [Test]
    public async Task AddReviewAsync_WhenExchangeNotFound_ReturnsFailure()
    {
        // Arrange
        var reviewerId = Guid.NewGuid();
        var request = new AddReviewRequest
        {
            ExchangeId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Great!"
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(null!));

        // Act
        var result = await _reviewService.AddReviewAsync(reviewerId, request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Exchange not found"));
    }

    [Test]
    public async Task AddReviewAsync_WhenExchangeNotAccepted_ReturnsFailure()
    {
        // Arrange
        var exchangeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var exchange = new Exchange
        {
            Id = exchangeId,
            OwnerId = Guid.NewGuid(),
            ReceiverId = reviewerId,
            Status = ExchangeStatus.Pending
        };

        var request = new AddReviewRequest
        {
            ExchangeId = exchangeId,
            Rating = 5,
            Comment = "Great!"
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(exchange));

        // Act
        var result = await _reviewService.AddReviewAsync(reviewerId, request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You can only review completed (accepted) exchanges"));
    }

    [Test]
    public async Task AddReviewAsync_WhenUserNotParticipant_ReturnsFailure()
    {
        // Arrange
        var exchangeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var exchange = new Exchange
        {
            Id = exchangeId,
            OwnerId = ownerId,
            ReceiverId = receiverId,
            Status = ExchangeStatus.Accepted
        };

        var request = new AddReviewRequest
        {
            ExchangeId = exchangeId,
            Rating = 5,
            Comment = "Great!"
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(exchange));

        // Act
        var result = await _reviewService.AddReviewAsync(reviewerId, request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You are not a participant in this exchange"));
    }

    [Test]
    public async Task AddReviewAsync_WhenAlreadyReviewed_ReturnsFailure()
    {
        // Arrange
        var exchangeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var exchange = new Exchange
        {
            Id = exchangeId,
            OwnerId = Guid.NewGuid(),
            ReceiverId = reviewerId,
            Status = ExchangeStatus.Accepted
        };

        var existingReview = new Review
        {
            Id = Guid.NewGuid(),
            ExchangeId = exchangeId,
            ReviewerId = reviewerId
        };

        var request = new AddReviewRequest
        {
            ExchangeId = exchangeId,
            Rating = 5,
            Comment = "Great!"
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(exchange));

        _reviewRepositoryMock
            .Setup(x => x.GetSingleAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Review>.Ok(existingReview));

        // Act
        var result = await _reviewService.AddReviewAsync(reviewerId, request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You have already reviewed this exchange"));
    }

    #endregion

    #region UpdateReviewAsync Tests

    [Test]
    public async Task UpdateReviewAsync_WithValidRequest_UpdatesReview()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var review = new Review
        {
            Id = reviewId,
            ExchangeId = Guid.NewGuid(),
            ReviewerId = reviewerId,
            ReviewedUserId = Guid.NewGuid(),
            Rating = 3,
            Comment = "OK"
        };

        var request = new UpdateReviewRequest
        {
            Rating = 5,
            Comment = "Actually it was great!"
        };

        _reviewRepositoryMock
            .Setup(x => x.GetSingleAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Review>.Ok(review));

        _reviewRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Review>(), default))
            .ReturnsAsync(Result.Ok());

        _reviewRepositoryMock
            .Setup(x => x.GetSingleAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                It.IsAny<List<Func<IQueryable<Review>, IIncludableQueryable<Review, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Review>.Ok(new Review
            {
                Id = reviewId,
                ExchangeId = review.ExchangeId,
                ReviewerId = reviewerId,
                Reviewer = new User { Id = reviewerId, FirstName = "John", LastName = "Doe" },
                ReviewedUserId = review.ReviewedUserId,
                ReviewedUser = new User { Id = review.ReviewedUserId, FirstName = "Jane", LastName = "Smith" },
                Rating = 5,
                Comment = "Actually it was great!",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }));

        // Act
        var result = await _reviewService.UpdateReviewAsync(reviewId, reviewerId, request);

        // Assert
        Assert.That(result.Success, Is.True);
        _reviewRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Review>(), default), Times.Once);
    }

    [Test]
    public async Task UpdateReviewAsync_WhenNotAuthorized_ReturnsFailure()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var review = new Review
        {
            Id = reviewId,
            ReviewerId = reviewerId,
            Rating = 3,
            Comment = "OK"
        };

        var request = new UpdateReviewRequest { Rating = 5 };

        _reviewRepositoryMock
            .Setup(x => x.GetSingleAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Review>.Ok(review));

        // Act
        var result = await _reviewService.UpdateReviewAsync(reviewId, otherUserId, request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You are not authorized to update this review"));
    }

    [Test]
    public async Task UpdateReviewAsync_WhenReviewNotFound_ReturnsFailure()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new UpdateReviewRequest { Rating = 5 };

        _reviewRepositoryMock
            .Setup(x => x.GetSingleAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Review>.Ok(null!));

        // Act
        var result = await _reviewService.UpdateReviewAsync(reviewId, userId, request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Review not found"));
    }

    #endregion

    #region DeleteReviewAsync Tests

    [Test]
    public async Task DeleteReviewAsync_WithValidRequest_DeletesReview()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var review = new Review
        {
            Id = reviewId,
            ReviewerId = reviewerId
        };

        _reviewRepositoryMock
            .Setup(x => x.GetSingleAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Review>.Ok(review));

        _reviewRepositoryMock
            .Setup(x => x.RemoveAsync(It.IsAny<Review>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _reviewService.DeleteReviewAsync(reviewId, reviewerId);

        // Assert
        Assert.That(result.Success, Is.True);
        _reviewRepositoryMock.Verify(x => x.RemoveAsync(It.IsAny<Review>(), default), Times.Once);
    }

    [Test]
    public async Task DeleteReviewAsync_WhenNotAuthorized_ReturnsFailure()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var review = new Review
        {
            Id = reviewId,
            ReviewerId = reviewerId
        };

        _reviewRepositoryMock
            .Setup(x => x.GetSingleAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Review>.Ok(review));

        // Act
        var result = await _reviewService.DeleteReviewAsync(reviewId, otherUserId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You are not authorized to delete this review"));
    }

    #endregion

    #region GetUserAverageRatingAsync Tests

    [Test]
    public async Task GetUserAverageRatingAsync_WithReviews_ReturnsAverage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviews = new List<Review>
        {
            new Review { Rating = 5 },
            new Review { Rating = 4 },
            new Review { Rating = 3 }
        };

        _reviewRepositoryMock
            .Setup(x => x.GetListAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                null,
                null,
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Review>>.Ok(reviews));

        // Act
        var result = await _reviewService.GetUserAverageRatingAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.EqualTo(4.0));
    }

    [Test]
    public async Task GetUserAverageRatingAsync_WithNoReviews_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _reviewRepositoryMock
            .Setup(x => x.GetListAsync<Review>(
                It.IsAny<Expression<Func<Review, bool>>>(),
                null,
                null,
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Review>>.Ok(new List<Review>()));

        // Act
        var result = await _reviewService.GetUserAverageRatingAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.EqualTo(0));
    }

    #endregion

    #region GetUserReviewCountAsync Tests

    [Test]
    public async Task GetUserReviewCountAsync_ReturnsCount()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<Expression<Func<Review, bool>>>(), default))
            .ReturnsAsync(Result<int>.Ok(5));

        // Act
        var result = await _reviewService.GetUserReviewCountAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.EqualTo(5));
    }

    #endregion
}

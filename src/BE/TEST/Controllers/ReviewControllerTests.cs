using API.Controllers;
using API.Services.Interfaces;
using DB.DTOs;
using DB.Repository.Utilites;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace TEST.Controllers;

[TestFixture]
public class ReviewControllerTests
{
    private Mock<IReviewService> _reviewServiceMock;
    private Mock<ILogger<ReviewController>> _loggerMock;
    private ReviewController _reviewController;

    [SetUp]
    public void SetUp()
    {
        _reviewServiceMock = new Mock<IReviewService>();
        _loggerMock = new Mock<ILogger<ReviewController>>();
        _reviewController = new ReviewController(_reviewServiceMock.Object, _loggerMock.Object);

        var identity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _reviewController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _reviewController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    #region GetReviews Tests

    [Test]
    public async Task GetReviews_WithoutFilter_ReturnsOkWithAllReviews()
    {
        // Arrange
        var reviews = new List<ReviewResponse>
        {
            new ReviewResponse
            {
                Id = Guid.NewGuid(),
                Rating = 5,
                Comment = "Great!",
                ReviewerFirstName = "John",
                ReviewerLastName = "Doe"
            },
            new ReviewResponse
            {
                Id = Guid.NewGuid(),
                Rating = 4,
                Comment = "Good",
                ReviewerFirstName = "Jane",
                ReviewerLastName = "Smith"
            }
        };

        _reviewServiceMock
            .Setup(x => x.GetReviewsAsync(null))
            .ReturnsAsync(Result<IEnumerable<ReviewResponse>>.Ok(reviews));

        // Act
        var result = await _reviewController.GetReviews();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        var returnedReviews = okResult.Value as IEnumerable<ReviewResponse>;
        Assert.That(returnedReviews, Is.Not.Null);
        Assert.That(returnedReviews!.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetReviews_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _reviewServiceMock
            .Setup(x => x.GetReviewsAsync(null))
            .ReturnsAsync(Result<IEnumerable<ReviewResponse>>.Fail("Database error"));

        // Act
        var result = await _reviewController.GetReviews();

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    #endregion

    #region GetReviewById Tests

    [Test]
    public async Task GetReviewById_WithValidId_ReturnsOkWithReview()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var review = new ReviewResponse
        {
            Id = reviewId,
            Rating = 5,
            Comment = "Excellent!"
        };

        _reviewServiceMock
            .Setup(x => x.GetReviewByIdAsync(reviewId))
            .ReturnsAsync(Result<ReviewResponse>.Ok(review));

        // Act
        var result = await _reviewController.GetReviewById(reviewId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var returnedReview = okResult!.Value as ReviewResponse;
        Assert.That(returnedReview!.Id, Is.EqualTo(reviewId));
    }

    [Test]
    public async Task GetReviewById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _reviewServiceMock
            .Setup(x => x.GetReviewByIdAsync(reviewId))
            .ReturnsAsync(Result<ReviewResponse>.Fail("Review not found"));

        // Act
        var result = await _reviewController.GetReviewById(reviewId);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    #endregion

    #region GetUserReviews Tests

    [Test]
    public async Task GetUserReviews_WithValidUserId_ReturnsOkWithReviews()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviews = new List<ReviewResponse>
        {
            new ReviewResponse { Id = Guid.NewGuid(), ReviewedUserId = userId, Rating = 5 }
        };

        _reviewServiceMock
            .Setup(x => x.GetReviewsAsync(It.Is<ReviewFilterRequest>(r => r.ReviewedUserId == userId)))
            .ReturnsAsync(Result<IEnumerable<ReviewResponse>>.Ok(reviews));

        // Act
        var result = await _reviewController.GetUserReviews(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var returnedReviews = okResult!.Value as IEnumerable<ReviewResponse>;
        Assert.That(returnedReviews!.Count(), Is.EqualTo(1));
    }

    #endregion

    #region GetUserRating Tests

    [Test]
    public async Task GetUserRating_WithValidUserId_ReturnsOkWithRating()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _reviewServiceMock
            .Setup(x => x.GetUserAverageRatingAsync(userId))
            .ReturnsAsync(Result<double>.Ok(4.5));

        _reviewServiceMock
            .Setup(x => x.GetUserReviewCountAsync(userId))
            .ReturnsAsync(Result<int>.Ok(10));

        // Act
        var result = await _reviewController.GetUserRating(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetUserRating_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _reviewServiceMock
            .Setup(x => x.GetUserAverageRatingAsync(userId))
            .ReturnsAsync(Result<double>.Fail("Error"));

        _reviewServiceMock
            .Setup(x => x.GetUserReviewCountAsync(userId))
            .ReturnsAsync(Result<int>.Ok(0));

        // Act
        var result = await _reviewController.GetUserRating(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    #endregion

    #region AddReview Tests

    [Test]
    public async Task AddReview_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new AddReviewRequest
        {
            ExchangeId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Great exchange!"
        };

        var createdReview = new ReviewResponse
        {
            Id = Guid.NewGuid(),
            ExchangeId = request.ExchangeId,
            ReviewerId = userId,
            Rating = 5,
            Comment = "Great exchange!"
        };

        _reviewServiceMock
            .Setup(x => x.AddReviewAsync(userId, request))
            .ReturnsAsync(Result<ReviewResponse>.Ok(createdReview));

        // Act
        var result = await _reviewController.AddReview(request);

        // Assert
        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = result as CreatedAtActionResult;
        Assert.That(createdResult!.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
    }

    [Test]
    public async Task AddReview_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - no authenticated user setup

        var request = new AddReviewRequest
        {
            ExchangeId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Great!"
        };

        // Act
        var result = await _reviewController.AddReview(request);

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task AddReview_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new AddReviewRequest
        {
            ExchangeId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Great!"
        };

        _reviewServiceMock
            .Setup(x => x.AddReviewAsync(userId, request))
            .ReturnsAsync(Result<ReviewResponse>.Fail("You have already reviewed this exchange"));

        // Act
        var result = await _reviewController.AddReview(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    #endregion

    #region UpdateReview Tests

    [Test]
    public async Task UpdateReview_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new UpdateReviewRequest
        {
            Rating = 4,
            Comment = "Updated comment"
        };

        var updatedReview = new ReviewResponse
        {
            Id = reviewId,
            Rating = 4,
            Comment = "Updated comment"
        };

        _reviewServiceMock
            .Setup(x => x.UpdateReviewAsync(reviewId, userId, request))
            .ReturnsAsync(Result<ReviewResponse>.Ok(updatedReview));

        // Act
        var result = await _reviewController.UpdateReview(reviewId, request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task UpdateReview_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new UpdateReviewRequest { Rating = 4 };

        _reviewServiceMock
            .Setup(x => x.UpdateReviewAsync(reviewId, userId, request))
            .ReturnsAsync(Result<ReviewResponse>.Fail("Review not found"));

        // Act
        var result = await _reviewController.UpdateReview(reviewId, request);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task UpdateReview_WhenNotAuthorized_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new UpdateReviewRequest { Rating = 4 };

        _reviewServiceMock
            .Setup(x => x.UpdateReviewAsync(reviewId, userId, request))
            .ReturnsAsync(Result<ReviewResponse>.Fail("You are not authorized to update this review"));

        // Act
        var result = await _reviewController.UpdateReview(reviewId, request);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    #endregion

    #region DeleteReview Tests

    [Test]
    public async Task DeleteReview_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        _reviewServiceMock
            .Setup(x => x.DeleteReviewAsync(reviewId, userId))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _reviewController.DeleteReview(reviewId);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteReview_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        _reviewServiceMock
            .Setup(x => x.DeleteReviewAsync(reviewId, userId))
            .ReturnsAsync(Result.Fail("Review not found"));

        // Act
        var result = await _reviewController.DeleteReview(reviewId);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task DeleteReview_WhenNotAuthorized_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        _reviewServiceMock
            .Setup(x => x.DeleteReviewAsync(reviewId, userId))
            .ReturnsAsync(Result.Fail("You are not authorized to delete this review"));

        // Act
        var result = await _reviewController.DeleteReview(reviewId);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    #endregion
}

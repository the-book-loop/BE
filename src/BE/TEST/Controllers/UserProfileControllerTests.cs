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
public class UserProfileControllerTests
{
    private Mock<IUserProfileService> _userProfileServiceMock;
    private Mock<ILogger<UserProfileController>> _loggerMock;
    private UserProfileController _userProfileController;

    [SetUp]
    public void SetUp()
    {
        _userProfileServiceMock = new Mock<IUserProfileService>();
        _loggerMock = new Mock<ILogger<UserProfileController>>();
        _userProfileController = new UserProfileController(_userProfileServiceMock.Object, _loggerMock.Object);

        var identity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _userProfileController.ControllerContext = new ControllerContext
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
        _userProfileController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    #region GetUserProfile Tests

    [Test]
    public async Task GetUserProfile_WithValidUserId_ReturnsOkWithProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = new UserPublicProfileResponse
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Location = "New York",
            AverageRating = 4.5,
            TotalReviews = 10,
            CompletedExchanges = 5,
            Books = new List<BookResponse>(),
            Reviews = new List<ReviewResponse>()
        };

        _userProfileServiceMock
            .Setup(x => x.GetUserPublicProfileAsync(userId, null))
            .ReturnsAsync(Result<UserPublicProfileResponse>.Ok(profile));

        // Act
        var result = await _userProfileController.GetUserProfile(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        var returnedProfile = okResult.Value as UserPublicProfileResponse;
        Assert.That(returnedProfile, Is.Not.Null);
        Assert.That(returnedProfile!.Id, Is.EqualTo(userId));
        Assert.That(returnedProfile.FirstName, Is.EqualTo("John"));
        Assert.That(returnedProfile.AverageRating, Is.EqualTo(4.5));
    }

    [Test]
    public async Task GetUserProfile_WithAuthenticatedUser_PassesRequesterId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        SetupAuthenticatedUser(requesterId);

        var profile = new UserPublicProfileResponse
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe"
        };

        _userProfileServiceMock
            .Setup(x => x.GetUserPublicProfileAsync(userId, requesterId))
            .ReturnsAsync(Result<UserPublicProfileResponse>.Ok(profile));

        // Act
        var result = await _userProfileController.GetUserProfile(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _userProfileServiceMock.Verify(x => x.GetUserPublicProfileAsync(userId, requesterId), Times.Once);
    }

    [Test]
    public async Task GetUserProfile_WithInvalidUserId_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userProfileServiceMock
            .Setup(x => x.GetUserPublicProfileAsync(userId, null))
            .ReturnsAsync(Result<UserPublicProfileResponse>.Fail("User not found"));

        // Act
        var result = await _userProfileController.GetUserProfile(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task GetUserProfile_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userProfileServiceMock
            .Setup(x => x.GetUserPublicProfileAsync(userId, null))
            .ReturnsAsync(Result<UserPublicProfileResponse>.Fail("Database error"));

        // Act
        var result = await _userProfileController.GetUserProfile(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetUserProfile_ReturnsProfileWithBooksAndReviews()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = new UserPublicProfileResponse
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            AverageRating = 4.0,
            TotalReviews = 2,
            CompletedExchanges = 3,
            Books = new List<BookResponse>
            {
                new BookResponse { Id = Guid.NewGuid(), Title = "Book 1" },
                new BookResponse { Id = Guid.NewGuid(), Title = "Book 2" }
            },
            Reviews = new List<ReviewResponse>
            {
                new ReviewResponse { Id = Guid.NewGuid(), Rating = 5, Comment = "Great!" },
                new ReviewResponse { Id = Guid.NewGuid(), Rating = 3, Comment = "OK" }
            }
        };

        _userProfileServiceMock
            .Setup(x => x.GetUserPublicProfileAsync(userId, null))
            .ReturnsAsync(Result<UserPublicProfileResponse>.Ok(profile));

        // Act
        var result = await _userProfileController.GetUserProfile(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var returnedProfile = okResult!.Value as UserPublicProfileResponse;

        Assert.That(returnedProfile!.Books.Count(), Is.EqualTo(2));
        Assert.That(returnedProfile.Reviews.Count(), Is.EqualTo(2));
    }

    #endregion

    #region GetExchangeableUsers Tests

    [Test]
    public async Task GetExchangeableUsers_WithAuthenticatedUser_ReturnsOkWithUsers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var users = new List<UserPublicProfileResponse>
        {
            new UserPublicProfileResponse
            {
                Id = Guid.NewGuid(),
                FirstName = "Partner1",
                LastName = "User",
                AverageRating = 4.5
            },
            new UserPublicProfileResponse
            {
                Id = Guid.NewGuid(),
                FirstName = "Partner2",
                LastName = "User",
                AverageRating = 5.0
            }
        };

        _userProfileServiceMock
            .Setup(x => x.GetExchangeableUsersAsync(userId))
            .ReturnsAsync(Result<IEnumerable<UserPublicProfileResponse>>.Ok(users));

        // Act
        var result = await _userProfileController.GetExchangeableUsers();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var returnedUsers = okResult!.Value as IEnumerable<UserPublicProfileResponse>;
        Assert.That(returnedUsers!.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetExchangeableUsers_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - no authenticated user

        // Act
        var result = await _userProfileController.GetExchangeableUsers();

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetExchangeableUsers_WithNoExchanges_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        _userProfileServiceMock
            .Setup(x => x.GetExchangeableUsersAsync(userId))
            .ReturnsAsync(Result<IEnumerable<UserPublicProfileResponse>>.Ok(new List<UserPublicProfileResponse>()));

        // Act
        var result = await _userProfileController.GetExchangeableUsers();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var returnedUsers = okResult!.Value as IEnumerable<UserPublicProfileResponse>;
        Assert.That(returnedUsers!.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetExchangeableUsers_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        _userProfileServiceMock
            .Setup(x => x.GetExchangeableUsersAsync(userId))
            .ReturnsAsync(Result<IEnumerable<UserPublicProfileResponse>>.Fail("Database error"));

        // Act
        var result = await _userProfileController.GetExchangeableUsers();

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetExchangeableUsers_ReturnsUsersWithRatings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var users = new List<UserPublicProfileResponse>
        {
            new UserPublicProfileResponse
            {
                Id = Guid.NewGuid(),
                FirstName = "Partner",
                LastName = "User",
                AverageRating = 4.5,
                TotalReviews = 10,
                CompletedExchanges = 5
            }
        };

        _userProfileServiceMock
            .Setup(x => x.GetExchangeableUsersAsync(userId))
            .ReturnsAsync(Result<IEnumerable<UserPublicProfileResponse>>.Ok(users));

        // Act
        var result = await _userProfileController.GetExchangeableUsers();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var returnedUsers = okResult!.Value as IEnumerable<UserPublicProfileResponse>;
        var firstUser = returnedUsers!.First();

        Assert.That(firstUser.AverageRating, Is.EqualTo(4.5));
        Assert.That(firstUser.TotalReviews, Is.EqualTo(10));
        Assert.That(firstUser.CompletedExchanges, Is.EqualTo(5));
    }

    #endregion
}

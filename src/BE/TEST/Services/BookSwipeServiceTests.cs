using API.Services.Interfaces;
using API.Services.Realisations;
using DB.DTOs;
using DB.Models;
using DB.Models.Enums;
using DB.Repository;
using DB.Repository.Utilites;
using Moq;

namespace TEST.Services;

[TestFixture]
public class BookSwipeServiceTests
{
    private Mock<IGenericRepository<BookSwipe>> _swipeRepositoryMock;
    private Mock<IGenericRepository<Book>> _bookRepositoryMock;
    private Mock<IGenericRepository<User>> _userRepositoryMock;
    private Mock<INotificationService> _notificationServiceMock;
    private BookSwipeService _bookSwipeService;

    [SetUp]
    public void SetUp()
    {
        _swipeRepositoryMock = new Mock<IGenericRepository<BookSwipe>>();
        _bookRepositoryMock = new Mock<IGenericRepository<Book>>();
        _userRepositoryMock = new Mock<IGenericRepository<User>>();
        _notificationServiceMock = new Mock<INotificationService>();
        _bookSwipeService = new BookSwipeService(
            _swipeRepositoryMock.Object,
            _bookRepositoryMock.Object,
            _userRepositoryMock.Object,
            _notificationServiceMock.Object);
    }

    [Test]
    public async Task GetSwipeableBooksAsync_ReturnsOnlyUnswipedBooks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var swipedBookId = Guid.NewGuid();
        var unswipedBookId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var existingSwipes = new List<BookSwipe>
        {
            new BookSwipe { UserId = userId, BookId = swipedBookId, SwipeType = SwipeType.Like }
        };

        var availableBooks = new List<Book>
        {
            new Book
            {
                Id = unswipedBookId,
                OwnerId = otherUserId,
                Owner = new User { Id = otherUserId, FirstName = "Owner", LastName = "User" },
                Language = "English",
                Title = "Unswiped Book",
                Author = "Author",
                Description = "Description",
                State = "Good",
                Genre = "Fiction",
                Created = DateTime.UtcNow
            }
        };

        _swipeRepositoryMock
            .Setup(x => x.GetListAsync<BookSwipe>(
                It.IsAny<System.Linq.Expressions.Expression<Func<BookSwipe, bool>>>(),
                null,
                null,
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<BookSwipe>>.Ok(existingSwipes));

        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(availableBooks));

        // Act
        var result = await _bookSwipeService.GetSwipeableBooksAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Count(), Is.EqualTo(1));
        Assert.That(result.Data.First().Id, Is.EqualTo(unswipedBookId));
    }

    [Test]
    public async Task GetSwipeableBooksAsync_ExcludesOwnBooks()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _swipeRepositoryMock
            .Setup(x => x.GetListAsync<BookSwipe>(
                It.IsAny<System.Linq.Expressions.Expression<Func<BookSwipe, bool>>>(),
                null,
                null,
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<BookSwipe>>.Ok(new List<BookSwipe>()));

        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(new List<Book>()));

        // Act
        var result = await _bookSwipeService.GetSwipeableBooksAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task SwipeBookAsync_WithLike_NotifiesOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var request = new SwipeRequest
        {
            BookId = bookId,
            SwipeType = SwipeType.Like
        };

        var book = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Owner = new User { Id = ownerId, FirstName = "Owner", LastName = "User" },
            Title = "Test Book"
        };

        var user = new User
        {
            Id = userId,
            FirstName = "Swiper",
            LastName = "User"
        };

        _swipeRepositoryMock
            .Setup(x => x.GetSingleAsync<BookSwipe>(
                It.IsAny<System.Linq.Expressions.Expression<Func<BookSwipe, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<BookSwipe>.Fail("Not found"));

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(book));

        _swipeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<BookSwipe>(), default))
            .ReturnsAsync(Result.Ok());

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<User>.Ok(user));

        // Act
        var result = await _bookSwipeService.SwipeBookAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.SwipeType, Is.EqualTo(SwipeType.Like));

        _notificationServiceMock.Verify(
            x => x.NotifyBookLikeAsync(ownerId, It.Is<BookLikeNotification>(n =>
                n.BookId == bookId &&
                n.LikedByUserId == userId &&
                n.Message.Contains("liked"))),
            Times.Once);
    }

    [Test]
    public async Task SwipeBookAsync_WithSkip_DoesNotNotifyOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var request = new SwipeRequest
        {
            BookId = bookId,
            SwipeType = SwipeType.Skip
        };

        var book = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Owner = new User { Id = ownerId, FirstName = "Owner", LastName = "User" },
            Title = "Test Book"
        };

        _swipeRepositoryMock
            .Setup(x => x.GetSingleAsync<BookSwipe>(
                It.IsAny<System.Linq.Expressions.Expression<Func<BookSwipe, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<BookSwipe>.Fail("Not found"));

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(book));

        _swipeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<BookSwipe>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _bookSwipeService.SwipeBookAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.SwipeType, Is.EqualTo(SwipeType.Skip));

        _notificationServiceMock.Verify(
            x => x.NotifyBookLikeAsync(It.IsAny<Guid>(), It.IsAny<BookLikeNotification>()),
            Times.Never);
    }

    [Test]
    public async Task SwipeBookAsync_WhenAlreadySwiped_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var request = new SwipeRequest
        {
            BookId = bookId,
            SwipeType = SwipeType.Like
        };

        var existingSwipe = new BookSwipe
        {
            UserId = userId,
            BookId = bookId,
            SwipeType = SwipeType.Skip
        };

        _swipeRepositoryMock
            .Setup(x => x.GetSingleAsync<BookSwipe>(
                It.IsAny<System.Linq.Expressions.Expression<Func<BookSwipe, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<BookSwipe>.Ok(existingSwipe));

        // Act
        var result = await _bookSwipeService.SwipeBookAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You have already swiped this book"));
    }

    [Test]
    public async Task SwipeBookAsync_WhenSwipingOwnBook_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var request = new SwipeRequest
        {
            BookId = bookId,
            SwipeType = SwipeType.Like
        };

        var book = new Book
        {
            Id = bookId,
            OwnerId = userId, // Same as the user trying to swipe
            Title = "Own Book"
        };

        _swipeRepositoryMock
            .Setup(x => x.GetSingleAsync<BookSwipe>(
                It.IsAny<System.Linq.Expressions.Expression<Func<BookSwipe, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<BookSwipe>.Fail("Not found"));

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(book));

        // Act
        var result = await _bookSwipeService.SwipeBookAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You cannot swipe your own book"));
    }

    [Test]
    public async Task SwipeBookAsync_WhenBookNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var request = new SwipeRequest
        {
            BookId = bookId,
            SwipeType = SwipeType.Like
        };

        _swipeRepositoryMock
            .Setup(x => x.GetSingleAsync<BookSwipe>(
                It.IsAny<System.Linq.Expressions.Expression<Func<BookSwipe, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<BookSwipe>.Fail("Not found"));

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Fail("Not found"));

        // Act
        var result = await _bookSwipeService.SwipeBookAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Book not found"));
    }

    [Test]
    public async Task GetUserLikesAsync_ReturnsOnlyLikes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var likedBookId = Guid.NewGuid();

        var likes = new List<BookSwipe>
        {
            new BookSwipe
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BookId = likedBookId,
                Book = new Book { Id = likedBookId, Title = "Liked Book" },
                SwipeType = SwipeType.Like,
                Created = DateTime.UtcNow
            }
        };

        _swipeRepositoryMock
            .Setup(x => x.GetListAsync<BookSwipe>(
                It.IsAny<System.Linq.Expressions.Expression<Func<BookSwipe, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<BookSwipe>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<BookSwipe, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<BookSwipe>>.Ok(likes));

        // Act
        var result = await _bookSwipeService.GetUserLikesAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Count(), Is.EqualTo(1));
        Assert.That(result.Data.First().SwipeType, Is.EqualTo(SwipeType.Like));
        Assert.That(result.Data.First().BookTitle, Is.EqualTo("Liked Book"));
    }
}

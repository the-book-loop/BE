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
public class ExchangeServiceTests
{
    private Mock<IGenericRepository<Exchange>> _exchangeRepositoryMock;
    private Mock<IGenericRepository<Book>> _bookRepositoryMock;
    private Mock<IGenericRepository<User>> _userRepositoryMock;
    private Mock<INotificationService> _notificationServiceMock;
    private ExchangeService _exchangeService;

    [SetUp]
    public void SetUp()
    {
        _exchangeRepositoryMock = new Mock<IGenericRepository<Exchange>>();
        _bookRepositoryMock = new Mock<IGenericRepository<Book>>();
        _userRepositoryMock = new Mock<IGenericRepository<User>>();
        _notificationServiceMock = new Mock<INotificationService>();
        _exchangeService = new ExchangeService(
            _exchangeRepositoryMock.Object,
            _bookRepositoryMock.Object,
            _userRepositoryMock.Object,
            _notificationServiceMock.Object);
    }

    [Test]
    public async Task GetExchangeDetailsAsync_WithValidRequest_ReturnsExchangeDetails()
    {
        // Arrange
        var exchangeId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var exchange = new Exchange
        {
            Id = exchangeId,
            BookId = bookId,
            Book = new Book { Id = bookId, Title = "Test Book" },
            OwnerId = ownerId,
            Owner = new User { Id = ownerId, FirstName = "Owner", LastName = "User" },
            ReceiverId = receiverId,
            Receiver = new User { Id = receiverId, FirstName = "Receiver", LastName = "User" },
            Status = ExchangeStatus.Pending,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        var requester = new User
        {
            Id = receiverId,
            FirstName = "Receiver",
            LastName = "User",
            Location = "Test Location",
            Description = "Test Description",
            Created = DateTime.UtcNow
        };

        var requesterBooks = new List<Book>
        {
            new Book
            {
                Id = Guid.NewGuid(),
                OwnerId = receiverId,
                Owner = requester,
                Title = "Requester's Book",
                Author = "Author",
                Language = "English",
                Description = "Description",
                State = "Good",
                Genre = "Fiction",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(exchange));

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<User>.Ok(requester));

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
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(requesterBooks));

        // Act
        var result = await _exchangeService.GetExchangeDetailsAsync(exchangeId, ownerId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Exchange.Id, Is.EqualTo(exchangeId));
        Assert.That(result.Data.RequesterProfile.Id, Is.EqualTo(receiverId));
        Assert.That(result.Data.RequesterProfile.FirstName, Is.EqualTo("Receiver"));
        Assert.That(result.Data.RequesterBooks.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetExchangeDetailsAsync_WhenUserNotAuthorized_ReturnsFailure()
    {
        // Arrange
        var exchangeId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();

        var exchange = new Exchange
        {
            Id = exchangeId,
            BookId = Guid.NewGuid(),
            Book = new Book { Id = Guid.NewGuid(), Title = "Test Book" },
            OwnerId = ownerId,
            Owner = new User { Id = ownerId, FirstName = "Owner", LastName = "User" },
            ReceiverId = receiverId,
            Receiver = new User { Id = receiverId, FirstName = "Receiver", LastName = "User" },
            Status = ExchangeStatus.Pending
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(exchange));

        // Act
        var result = await _exchangeService.GetExchangeDetailsAsync(exchangeId, unauthorizedUserId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You are not authorized to view this exchange details"));
    }

    [Test]
    public async Task GetExchangeDetailsAsync_WhenExchangeNotFound_ReturnsFailure()
    {
        // Arrange
        var exchangeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Fail("Not found"));

        // Act
        var result = await _exchangeService.GetExchangeDetailsAsync(exchangeId, userId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Exchange not found"));
    }

    [Test]
    public async Task UpdateExchangeAsync_WhenOwnerAccepts_NotifiesBothUsers()
    {
        // Arrange
        var exchangeId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var exchange = new Exchange
        {
            Id = exchangeId,
            BookId = bookId,
            OwnerId = ownerId,
            ReceiverId = receiverId,
            Status = ExchangeStatus.Pending
        };

        var updateRequest = new UpdateExchangeRequest
        {
            Status = ExchangeStatus.Accepted
        };

        var updatedExchange = new Exchange
        {
            Id = exchangeId,
            BookId = bookId,
            Book = new Book { Id = bookId, Title = "Test Book" },
            OwnerId = ownerId,
            Owner = new User { Id = ownerId, FirstName = "Owner", LastName = "User" },
            ReceiverId = receiverId,
            Receiver = new User { Id = receiverId, FirstName = "Receiver", LastName = "User" },
            Status = ExchangeStatus.Accepted,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Exchange, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(exchange));

        _exchangeRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Exchange>(), default))
            .ReturnsAsync(Result.Ok());

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(updatedExchange));

        // Act
        var result = await _exchangeService.UpdateExchangeAsync(exchangeId, ownerId, updateRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Status, Is.EqualTo(ExchangeStatus.Accepted));

        // Verify both users were notified
        _notificationServiceMock.Verify(
            x => x.NotifyUserAsync(receiverId, It.Is<ExchangeNotification>(n => n.Message.Contains("accepted"))),
            Times.Once);
        _notificationServiceMock.Verify(
            x => x.NotifyUserAsync(ownerId, It.Is<ExchangeNotification>(n => n.Message.Contains("accepted"))),
            Times.Once);
    }

    [Test]
    public async Task UpdateExchangeAsync_WhenOwnerDeclines_NotifiesBothUsers()
    {
        // Arrange
        var exchangeId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var exchange = new Exchange
        {
            Id = exchangeId,
            BookId = bookId,
            OwnerId = ownerId,
            ReceiverId = receiverId,
            Status = ExchangeStatus.Pending
        };

        var updateRequest = new UpdateExchangeRequest
        {
            Status = ExchangeStatus.Declined
        };

        var updatedExchange = new Exchange
        {
            Id = exchangeId,
            BookId = bookId,
            Book = new Book { Id = bookId, Title = "Test Book" },
            OwnerId = ownerId,
            Owner = new User { Id = ownerId, FirstName = "Owner", LastName = "User" },
            ReceiverId = receiverId,
            Receiver = new User { Id = receiverId, FirstName = "Receiver", LastName = "User" },
            Status = ExchangeStatus.Declined,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Exchange, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(exchange));

        _exchangeRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Exchange>(), default))
            .ReturnsAsync(Result.Ok());

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(updatedExchange));

        // Act
        var result = await _exchangeService.UpdateExchangeAsync(exchangeId, ownerId, updateRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Status, Is.EqualTo(ExchangeStatus.Declined));

        // Verify both users were notified with rejection message
        _notificationServiceMock.Verify(
            x => x.NotifyUserAsync(receiverId, It.Is<ExchangeNotification>(n => n.Message.Contains("declined"))),
            Times.Once);
        _notificationServiceMock.Verify(
            x => x.NotifyUserAsync(ownerId, It.Is<ExchangeNotification>(n => n.Message.Contains("declined"))),
            Times.Once);
    }

    [Test]
    public async Task AddExchangeAsync_WithValidRequest_NotifiesOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var exchangeId = Guid.NewGuid();

        var addRequest = new AddExchangeRequest
        {
            BookId = bookId,
            OwnerId = ownerId
        };

        var createdExchange = new Exchange
        {
            Id = exchangeId,
            BookId = bookId,
            Book = new Book { Id = bookId, Title = "Test Book" },
            OwnerId = ownerId,
            Owner = new User { Id = ownerId, FirstName = "Owner", LastName = "User" },
            ReceiverId = receiverId,
            Receiver = new User { Id = receiverId, FirstName = "Receiver", LastName = "User" },
            Status = ExchangeStatus.Pending,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _exchangeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Exchange>(), default))
            .ReturnsAsync(Result.Ok())
            .Callback<Exchange, CancellationToken>((e, _) => e.Id = exchangeId);

        _exchangeRepositoryMock
            .Setup(x => x.GetSingleAsync<Exchange>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Exchange, bool>>>(),
                It.IsAny<List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Exchange>.Ok(createdExchange));

        // Act
        var result = await _exchangeService.AddExchangeAsync(receiverId, addRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Status, Is.EqualTo(ExchangeStatus.Pending));

        // Verify owner was notified
        _notificationServiceMock.Verify(
            x => x.NotifyUserAsync(ownerId, It.Is<ExchangeNotification>(n => n.Message.Contains("exchange proposal"))),
            Times.Once);
    }

    [Test]
    public async Task AddExchangeAsync_WhenOwnerEqualsReceiver_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var addRequest = new AddExchangeRequest
        {
            BookId = bookId,
            OwnerId = userId
        };

        // Act
        var result = await _exchangeService.AddExchangeAsync(userId, addRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You cannot create exchange yourself"));

        _exchangeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Exchange>(), default), Times.Never);
    }
}

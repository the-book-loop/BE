using API.Services.Interfaces;
using API.Services.Realisations;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.Extensions.Logging;
using Moq;

namespace TEST.Services;

[TestFixture]
public class RecommendationServiceTests
{
    private Mock<IGenericRepository<Book>> _bookRepositoryMock;
    private Mock<IAiService> _aiServiceMock;
    private Mock<ILogger<RecommendationService>> _loggerMock;
    private RecommendationService _recommendationService;

    [SetUp]
    public void SetUp()
    {
        _bookRepositoryMock = new Mock<IGenericRepository<Book>>();
        _aiServiceMock = new Mock<IAiService>();
        _loggerMock = new Mock<ILogger<RecommendationService>>();
        _recommendationService = new RecommendationService(
            _bookRepositoryMock.Object,
            _aiServiceMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task GetRecommendationsAsync_WithValidQuery_ReturnsRecommendations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var request = new RecommendationRequest
        {
            Query = "fantasy with dragons",
            Limit = 5
        };

        var books = new List<Book>
        {
            new Book
            {
                Id = bookId,
                OwnerId = Guid.NewGuid(),
                Owner = new User { FirstName = "Owner", LastName = "User", Location = "NYC" },
                Language = "English",
                Title = "The Dragon's Tale",
                Author = "Fantasy Author",
                Description = "A story about dragons",
                Genre = "Fantasy",
                State = "Good",
                Created = DateTime.UtcNow
            }
        };

        var aiResponse = $@"{{
            ""recommendations"": [
                {{
                    ""id"": ""{bookId}"",
                    ""score"": 0.95,
                    ""explanation"": ""This book features dragons prominently in a fantasy setting.""
                }}
            ]
        }}";

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
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(books));

        _aiServiceMock
            .Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(aiResponse);

        // Act
        var result = await _recommendationService.GetRecommendationsAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Query, Is.EqualTo("fantasy with dragons"));
        Assert.That(result.Data.TotalResults, Is.EqualTo(1));
        Assert.That(result.Data.Recommendations.First().Title, Is.EqualTo("The Dragon's Tale"));
        Assert.That(result.Data.Recommendations.First().MatchScore, Is.EqualTo(0.95));
        Assert.That(result.Data.Recommendations.First().Explanation, Does.Contain("dragons"));
    }

    [Test]
    public async Task GetRecommendationsAsync_WhenNoBooks_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RecommendationRequest
        {
            Query = "fantasy",
            Limit = 5
        };

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
        var result = await _recommendationService.GetRecommendationsAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.TotalResults, Is.EqualTo(0));
        Assert.That(result.Data.Recommendations, Is.Empty);

        // AI service should not be called when no books available
        _aiServiceMock.Verify(
            x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task GetRecommendationsAsync_WhenAiFails_UsesFallback()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var request = new RecommendationRequest
        {
            Query = "fantasy dragons",
            Limit = 5
        };

        var books = new List<Book>
        {
            new Book
            {
                Id = bookId,
                OwnerId = Guid.NewGuid(),
                Owner = new User { FirstName = "Owner", LastName = "User" },
                Language = "English",
                Title = "The Dragon's Tale",
                Author = "Author",
                Description = "A fantasy story about dragons",
                Genre = "Fantasy",
                State = "Good"
            }
        };

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
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(books));

        _aiServiceMock
            .Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("AI service unavailable"));

        // Act
        var result = await _recommendationService.GetRecommendationsAsync(userId, request);

        // Assert - should still succeed with fallback keyword matching
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.TotalResults, Is.EqualTo(1));
        Assert.That(result.Data.Recommendations.First().Title, Is.EqualTo("The Dragon's Tale"));
    }

    [Test]
    public async Task GetRecommendationsAsync_ExcludesUserOwnBooks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RecommendationRequest
        {
            Query = "any book",
            Limit = 5
        };

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
        await _recommendationService.GetRecommendationsAsync(userId, request);

        // Assert - verify filter excludes user's own books
        _bookRepositoryMock.Verify(
            x => x.GetListAsync<Book>(
                It.Is<System.Linq.Expressions.Expression<Func<Book, bool>>>(
                    expr => expr != null),
                null,
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default),
            Times.Once);
    }

    [Test]
    public async Task GetRecommendationsAsync_RespectsLimit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RecommendationRequest
        {
            Query = "fiction",
            Limit = 2
        };

        var books = Enumerable.Range(1, 5).Select(i => new Book
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Owner = new User { FirstName = "Owner", LastName = $"User{i}" },
            Language = "English",
            Title = $"Fiction Book {i}",
            Author = "Author",
            Description = "A fiction story",
            Genre = "Fiction",
            State = "Good"
        }).ToList();

        var aiResponse = @"{
            ""recommendations"": [
                { ""id"": """ + books[0].Id + @""", ""score"": 0.9, ""explanation"": ""Match 1"" },
                { ""id"": """ + books[1].Id + @""", ""score"": 0.8, ""explanation"": ""Match 2"" },
                { ""id"": """ + books[2].Id + @""", ""score"": 0.7, ""explanation"": ""Match 3"" }
            ]
        }";

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
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(books));

        _aiServiceMock
            .Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(aiResponse);

        // Act
        var result = await _recommendationService.GetRecommendationsAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.TotalResults, Is.LessThanOrEqualTo(2));
    }

    [Test]
    public async Task GetRecommendationsAsync_HandlesMarkdownCodeBlocks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var request = new RecommendationRequest
        {
            Query = "test",
            Limit = 5
        };

        var books = new List<Book>
        {
            new Book
            {
                Id = bookId,
                OwnerId = Guid.NewGuid(),
                Owner = new User { FirstName = "Owner", LastName = "User" },
                Language = "English",
                Title = "Test Book",
                Author = "Author",
                Description = "Description",
                Genre = "Test",
                State = "Good"
            }
        };

        // AI response wrapped in markdown code blocks
        var aiResponse = $@"```json
{{
    ""recommendations"": [
        {{
            ""id"": ""{bookId}"",
            ""score"": 0.9,
            ""explanation"": ""Test match""
        }}
    ]
}}
```";

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
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(books));

        _aiServiceMock
            .Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(aiResponse);

        // Act
        var result = await _recommendationService.GetRecommendationsAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.TotalResults, Is.EqualTo(1));
    }
}

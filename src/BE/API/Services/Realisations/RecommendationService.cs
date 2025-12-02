using System.Text.Json;
using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Realisations;

public class RecommendationService : IRecommendationService
{
    private readonly IGenericRepository<Book> _bookRepository;
    private readonly IAiService _aiService;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        IGenericRepository<Book> bookRepository,
        IAiService aiService,
        ILogger<RecommendationService> logger)
    {
        _bookRepository = bookRepository;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<Result<RecommendationResponse>> GetRecommendationsAsync(Guid userId, RecommendationRequest request)
    {
        // Get all available books (excluding user's own books)
        var includes = new List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>
        {
            query => query.Include(b => b.Owner)
        };

        var booksResult = await _bookRepository.GetListAsync<Book>(
            filter: b => b.OwnerId != userId,
            includes: includes,
            selector: null
        );

        if (!booksResult.Success)
        {
            return Result<RecommendationResponse>.Fail(booksResult.Error);
        }

        var books = booksResult.Data.ToList();

        if (!books.Any())
        {
            return Result<RecommendationResponse>.Ok(new RecommendationResponse
            {
                Query = request.Query,
                TotalResults = 0,
                Recommendations = new List<BookRecommendation>()
            });
        }

        // Create a condensed book catalog for the AI
        var bookCatalog = books.Select(b => new
        {
            id = b.Id.ToString(),
            title = b.Title,
            author = b.Author,
            description = b.Description,
            genre = b.Genre,
            language = b.Language ?? "Unknown",
            state = b.State
        }).ToList();

        var catalogJson = JsonSerializer.Serialize(bookCatalog);

        // Create the AI prompt
        var systemPrompt = @"You are a book recommendation assistant. Given a user's preference query and a catalog of available books, recommend the most relevant books.

For each recommended book, provide:
1. The book ID (exactly as given)
2. A match score from 0.0 to 1.0 indicating relevance
3. A brief explanation (1-2 sentences) of why this book matches the user's preferences

Respond ONLY with valid JSON in this exact format:
{
  ""recommendations"": [
    {
      ""id"": ""book-guid-here"",
      ""score"": 0.95,
      ""explanation"": ""Brief explanation here""
    }
  ]
}

Important rules:
- Only recommend books from the provided catalog
- Order by relevance (highest score first)
- If no books match well, return an empty recommendations array
- Maximum recommendations: as specified by the user
- Be creative in matching - consider genre, themes, writing style, etc.";

        var userPrompt = $@"User preference: ""{request.Query}""

Maximum recommendations needed: {request.Limit}

Available books catalog:
{catalogJson}

Analyze the catalog and recommend the most relevant books based on the user's preference.";

        try
        {
            var aiResponse = await _aiService.GetCompletionAsync(systemPrompt, userPrompt);

            // Parse AI response
            var recommendations = ParseAiRecommendations(aiResponse, books, request.Limit);

            return Result<RecommendationResponse>.Ok(new RecommendationResponse
            {
                Query = request.Query,
                TotalResults = recommendations.Count,
                Recommendations = recommendations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI recommendations for query: {Query}", request.Query);

            // Fallback to simple keyword matching if AI fails
            var fallbackRecommendations = GetFallbackRecommendations(books, request.Query, request.Limit);

            return Result<RecommendationResponse>.Ok(new RecommendationResponse
            {
                Query = request.Query,
                TotalResults = fallbackRecommendations.Count,
                Recommendations = fallbackRecommendations
            });
        }
    }

    private List<BookRecommendation> ParseAiRecommendations(string aiResponse, List<Book> books, int limit)
    {
        var recommendations = new List<BookRecommendation>();

        try
        {
            // Clean the response - remove markdown code blocks if present
            var cleanedResponse = aiResponse.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            using var doc = JsonDocument.Parse(cleanedResponse);
            var recs = doc.RootElement.GetProperty("recommendations");

            foreach (var rec in recs.EnumerateArray())
            {
                if (recommendations.Count >= limit) break;

                var idString = rec.GetProperty("id").GetString();
                if (!Guid.TryParse(idString, out var bookId)) continue;

                var book = books.FirstOrDefault(b => b.Id == bookId);
                if (book == null) continue;

                var score = rec.GetProperty("score").GetDouble();
                var explanation = rec.GetProperty("explanation").GetString() ?? "Matches your preferences";

                recommendations.Add(CreateBookRecommendation(book, score, explanation));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response, using fallback");
        }

        return recommendations;
    }

    private List<BookRecommendation> GetFallbackRecommendations(List<Book> books, string query, int limit)
    {
        var queryTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var scoredBooks = books.Select(book =>
        {
            var searchableText = $"{book.Title} {book.Author} {book.Description} {book.Genre}".ToLower();
            var matchCount = queryTerms.Count(term => searchableText.Contains(term));
            var score = queryTerms.Length > 0 ? (double)matchCount / queryTerms.Length : 0;
            return new { Book = book, Score = score };
        })
        .Where(x => x.Score > 0)
        .OrderByDescending(x => x.Score)
        .Take(limit)
        .ToList();

        return scoredBooks.Select(x => CreateBookRecommendation(
            x.Book,
            x.Score,
            $"Matches {(int)(x.Score * 100)}% of your search terms"
        )).ToList();
    }

    private BookRecommendation CreateBookRecommendation(Book book, double score, string explanation)
    {
        return new BookRecommendation
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Description = book.Description,
            Genre = book.Genre,
            State = book.State,
            Language = book.Language ?? string.Empty,
            OwnerId = book.OwnerId,
            OwnerFirstName = book.Owner?.FirstName ?? string.Empty,
            OwnerLastName = book.Owner?.LastName ?? string.Empty,
            OwnerLocation = book.Owner?.Location,
            MatchScore = Math.Round(score, 2),
            Explanation = explanation
        };
    }
}

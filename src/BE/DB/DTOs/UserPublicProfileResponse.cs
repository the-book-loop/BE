namespace DB.DTOs;

public class UserPublicProfileResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Img { get; set; }
    public string? Description { get; set; }
    public DateTime Created { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int CompletedExchanges { get; set; }
    public IEnumerable<BookResponse> Books { get; set; } = new List<BookResponse>();
    public IEnumerable<ReviewResponse> Reviews { get; set; } = new List<ReviewResponse>();
}

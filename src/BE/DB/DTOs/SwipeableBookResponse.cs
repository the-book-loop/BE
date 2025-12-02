namespace DB.DTOs;

public class SwipeableBookResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string OwnerFirstName { get; set; } = string.Empty;
    public string OwnerLastName { get; set; } = string.Empty;
    public string? OwnerLocation { get; set; }
    public string? OwnerImg { get; set; }
    public DateTime Created { get; set; }
}

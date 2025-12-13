namespace DB.DTOs;

public class ReviewResponse
{
    public Guid Id { get; set; }
    public Guid ExchangeId { get; set; }
    public Guid ReviewerId { get; set; }
    public string ReviewerFirstName { get; set; } = string.Empty;
    public string ReviewerLastName { get; set; } = string.Empty;
    public string? ReviewerImg { get; set; }
    public Guid ReviewedUserId { get; set; }
    public string ReviewedUserFirstName { get; set; } = string.Empty;
    public string ReviewedUserLastName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

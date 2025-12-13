namespace DB.DTOs;

public class ReviewFilterRequest
{
    public Guid? ReviewerId { get; set; }
    public Guid? ReviewedUserId { get; set; }
    public Guid? ExchangeId { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
}

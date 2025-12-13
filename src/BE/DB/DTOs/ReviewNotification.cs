namespace DB.DTOs;

public class ReviewNotification
{
    public string Message { get; set; } = string.Empty;
    public ReviewResponse Data { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;

namespace DB.DTOs;

public class UpdateReviewRequest
{
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int? Rating { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }
}

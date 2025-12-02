using System.ComponentModel.DataAnnotations;

namespace DB.DTOs;

public class AddBookRequest
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Language { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Genre { get; set; } = string.Empty;
}

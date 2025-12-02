using System.ComponentModel.DataAnnotations;

namespace DB.DTOs;

public class UpdateBookRequest
{
    [StringLength(100)]
    public string? Title { get; set; }

    [StringLength(100)]
    public string? Author {  get; set; }

    [StringLength(50)]
    public string? Language { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(50)]
    public string? Genre { get; set; }
}

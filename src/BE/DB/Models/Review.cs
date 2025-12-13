using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DB.Models;

public class Review : BaseEntity
{
    [Required]
    [Column("exchange_id", TypeName = "uuid")]
    public Guid ExchangeId { get; set; }

    [ForeignKey("ExchangeId")]
    public Exchange Exchange { get; set; }

    [Required]
    [Column("reviewer_id", TypeName = "uuid")]
    public Guid ReviewerId { get; set; }

    [ForeignKey("ReviewerId")]
    public User Reviewer { get; set; }

    [Required]
    [Column("reviewed_user_id", TypeName = "uuid")]
    public Guid ReviewedUserId { get; set; }

    [ForeignKey("ReviewedUserId")]
    public User ReviewedUser { get; set; }

    [Required]
    [Column("rating", TypeName = "integer")]
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(500)]
    [Column("comment", TypeName = "varchar(500)")]
    public string? Comment { get; set; }
}

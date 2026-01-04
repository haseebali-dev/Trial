using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

[Table("otps", Schema = "greenchain")]
public class Otp
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Column("phone_number")]
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Column("code")]
    [Required]
    [MaxLength(6)]
    public string Code { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("is_used")]
    public bool IsUsed { get; set; } = false;
}

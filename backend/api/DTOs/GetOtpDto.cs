using System.ComponentModel.DataAnnotations;

namespace api.DTOs;

public class GetOtpDto
{
    [Required]
    public string MobileNumber { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace api.DTOs;

public class VerifyOtpDto
{
    [Required]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}

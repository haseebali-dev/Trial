using api.Models;
using api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OtpController : ControllerBase
{
    private readonly ApplicationDBContext _context;

    public OtpController(ApplicationDBContext context)
    {
        _context = context;
    }

    [HttpGet("get-code")]
    public async Task<IActionResult> GetCode([FromQuery] GetOtpDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        var otp = new Otp
        {
            PhoneNumber = request.MobileNumber,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5), 
            IsUsed = false
        };

        _context.Otps.Add(otp);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "OTP generated successfully", Code = code });
    }

    [HttpGet("verify-code")]
    public async Task<IActionResult> VerifyCode([FromQuery] VerifyOtpDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var otp = await _context.Otps
            .Where(o => o.PhoneNumber == request.MobileNumber && o.Code == request.Code && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            return BadRequest("Invalid OTP");
        }

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest("OTP has expired");
        }

        otp.IsUsed = true;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "OTP verified successfully" });
    }
}

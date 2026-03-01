using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SourceFlow.Api.Data;
using SourceFlow.Api.Dtos;
using SourceFlow.Api.Models;
using SourceFlow.Api.Services;

namespace SourceFlow.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly EmailService _email;

    public AuthController(AppDbContext db, IConfiguration config, EmailService email)
    {
        _db = db;
        _config = config;
        _email = email;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { error = "Email already registered" });

        var (verificationOtp, verificationOtpHash, verificationExpiry) = CreateEmailVerificationOtp();
        var user = new User
        {
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            CreditsBalance = 50,
            Country = req.Country ?? "IN",
            IsEmailVerified = false,
            EmailVerificationTokenHash = verificationOtpHash,
            EmailVerificationExpiry = verificationExpiry,
            EmailVerificationSentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = user.Id,
            CreditsChanged = 50,
            Type = "signup_bonus",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var sent = await _email.SendEmailVerificationOtpAsync(user.Email, verificationOtp);
        if (!sent)
        {
            var signupBonus = await _db.CreditTransactions
                .Where(t => t.UserId == user.Id && t.Type == "signup_bonus")
                .ToListAsync();
            if (signupBonus.Count > 0)
                _db.CreditTransactions.RemoveRange(signupBonus);
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return StatusCode(503, new { error = "Could not send OTP email. Try again in a moment." });
        }

        return Ok(new AuthResponse
        {
            Token = GenerateJwt(user),
            Email = user.Email,
            Credits = user.CreditsBalance,
            Country = user.Country
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials" });

        return Ok(new AuthResponse
        {
            Token = GenerateJwt(user),
            Email = user.Email,
            Credits = user.CreditsBalance,
            Country = user.Country
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null)
            return Ok(new { message = "If that email exists, we've sent a reset link." });

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        user.PasswordResetToken = token;
        user.PasswordResetExpiry = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        var sent = await _email.SendPasswordResetAsync(user.Email, resetLink);
        if (!sent)
            return StatusCode(500, new { error = "Email not configured. Set Brevo:ApiKey and Brevo:FromEmail." });

        return Ok(new { message = "If that email exists, we've sent a reset link." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == req.Token &&
            u.PasswordResetExpiry != null &&
            u.PasswordResetExpiry > DateTime.UtcNow);

        if (user == null)
            return BadRequest(new { error = "Invalid or expired reset link. Request a new one." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpiry = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password reset. You can now log in." });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public IActionResult VerifyEmail([FromBody] VerifyEmailRequest req)
    {
        return BadRequest(new { error = "Link verification is disabled. Use OTP verification from the extension popup." });
    }

    [HttpPost("verify-email-otp")]
    [Authorize]
    public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpRequest req)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (user.IsEmailVerified)
            return Ok(new { message = "Email already verified." });

        var otpHash = HashToken(req.Otp);
        if (user.EmailVerificationTokenHash != otpHash ||
            user.EmailVerificationExpiry == null ||
            user.EmailVerificationExpiry <= DateTime.UtcNow)
        {
            return BadRequest(new { error = "Invalid or expired OTP. Request a new code." });
        }

        user.IsEmailVerified = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationExpiry = null;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Email verified successfully." });
    }

    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<IActionResult> ResendVerification()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (user.IsEmailVerified)
            return Ok(new { message = "Email is already verified." });

        if (user.EmailVerificationSentAt.HasValue && user.EmailVerificationSentAt.Value > DateTime.UtcNow.AddMinutes(-1))
            return StatusCode(429, new { error = "Please wait before requesting another verification email." });

        var (otp, otpHash, expiry) = CreateEmailVerificationOtp();
        user.EmailVerificationTokenHash = otpHash;
        user.EmailVerificationExpiry = expiry;
        user.EmailVerificationSentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var sent = await _email.SendEmailVerificationOtpAsync(user.Email, otp);
        if (!sent)
            return StatusCode(500, new { error = "Email not configured. Set Brevo:ApiKey and Brevo:FromEmail." });

        return Ok(new { message = "Verification OTP sent." });
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var expiryDays = _config.GetValue("Jwt:ExpiryDays", 30);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static (string Otp, string OtpHash, DateTime Expiry) CreateEmailVerificationOtp()
    {
        var otp = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
        return (otp, HashToken(otp), DateTime.UtcNow.AddMinutes(10));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

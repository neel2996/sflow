using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceFlow.Api.Data;
using SourceFlow.Api.Dtos;

namespace SourceFlow.Api.Controllers;

[ApiController]
[Route("user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AppDbContext _db;

    public UserController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            credits = user.CreditsBalance,
            country = user.Country,
            createdAt = user.CreatedAt,
            unlimitedAccessTill = user.UnlimitedAccessTill
        });
    }

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateCountry([FromBody] UpdateCountryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Country))
            return BadRequest(new { error = "Country is required" });
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        user.Country = req.Country.Trim().ToUpperInvariant();
        if (user.Country == "OTHER") user.Country = "US"; // Normalize for payment logic
        await _db.SaveChangesAsync();
        return Ok(new { country = user.Country });
    }
}

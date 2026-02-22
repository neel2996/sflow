using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceFlow.Api.Data;
using SourceFlow.Api.Dtos;
using SourceFlow.Api.Models;

namespace SourceFlow.Api.Controllers;

[ApiController]
[Route("feedback")]
public class FeedbackController : ControllerBase
{
    private readonly AppDbContext _db;

    public FeedbackController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Submit feedback. If user is logged in, UserId is attached from JWT.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Submit([FromBody] SubmitFeedbackRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "Message is required" });

        var validTypes = new[] { "feedback", "bug", "feature" };
        if (!validTypes.Contains(req.Type))
            return BadRequest(new { error = "Type must be feedback, bug, or feature" });

        int? userId = null;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedId))
            userId = parsedId;

        var feedback = new Feedback
        {
            UserId = userId,
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
            Message = req.Message.Trim(),
            Type = req.Type,
            CreatedAt = DateTime.UtcNow
        };

        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, id = feedback.Id });
    }

    /// <summary>
    /// Get all feedback (admin future-ready). Ordered by CreatedAt desc.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List()
    {
        var items = await _db.Feedbacks
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                f.UserId,
                f.Email,
                f.Message,
                f.Type,
                f.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }
}

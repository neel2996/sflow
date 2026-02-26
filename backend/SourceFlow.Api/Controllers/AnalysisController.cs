using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceFlow.Api.Data;
using SourceFlow.Api.Dtos;
using SourceFlow.Api.Services;

namespace SourceFlow.Api.Controllers;

[ApiController]
[Route("analysis")]
[Authorize]
public class AnalysisController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AiService _ai;
    private readonly CreditService _credits;
    private readonly CacheService _cache;

    public AnalysisController(AppDbContext db, AiService ai, CreditService credits, CacheService cache)
    {
        _db = db;
        _ai = ai;
        _credits = credits;
        _cache = cache;
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan(ScanRequest req)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == req.JobId && j.UserId == userId);
        if (job == null)
            return NotFound(new { error = "Job not found" });

        var cached = await _cache.GetCachedResult(req.ProfileUrl, req.JobId);
        if (cached != null)
            return Content(cached.JsonResult, "application/json");

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Unlimited access: no credit deduction
        if (user.UnlimitedAccessTill.HasValue && user.UnlimitedAccessTill.Value > DateTime.UtcNow)
        {
            // Allow usage without deducting credits
        }
        else
        {
            var currentCredits = await _credits.GetCredits(userId);
            if (currentCredits <= 0)
                return StatusCode(403, new { error = "No credits remaining", code = "PAYWALL" });

            var deducted = await _credits.DeductCredit(userId);
            if (!deducted)
                return StatusCode(403, new { error = "Failed to deduct credit", code = "PAYWALL" });
        }

        ScanResult result;
        try
        {
            result = await _ai.AnalyzeProfile(job.Description, req.ProfileText);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("OpenAI"))
        {
            // Refund the credit since we didn't complete the analysis (only if we deducted)
            if (!(user.UnlimitedAccessTill.HasValue && user.UnlimitedAccessTill.Value > DateTime.UtcNow))
                await _credits.AddCredits(userId, 1, "Refund");
            return StatusCode(503, new { error = ex.Message, code = "AI_SERVICE_ERROR" });
        }

        // Always use client-computed experience — AI arithmetic is unreliable
        if (req.ComputedExperienceYears.HasValue && req.ComputedExperienceYears.Value > 0)
            result.TotalExperienceYears = req.ComputedExperienceYears.Value;

        var jsonResult = JsonSerializer.Serialize(result);
        await _cache.SaveResult(req.ProfileUrl, req.JobId, jsonResult, result.MatchScore);

        return Ok(result);
    }
}

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

        var currentCredits = await _credits.GetCredits(userId);
        if (currentCredits <= 0)
            return BadRequest(new { error = "No credits remaining" });

        var deducted = await _credits.DeductCredit(userId);
        if (!deducted)
            return BadRequest(new { error = "Failed to deduct credit" });

        var result = await _ai.AnalyzeProfile(job.Description, req.ProfileText);

        // Always use client-computed experience — AI arithmetic is unreliable
        if (req.ComputedExperienceYears.HasValue && req.ComputedExperienceYears.Value > 0)
            result.TotalExperienceYears = req.ComputedExperienceYears.Value;

        var jsonResult = JsonSerializer.Serialize(result);
        await _cache.SaveResult(req.ProfileUrl, req.JobId, jsonResult, result.MatchScore);

        return Ok(result);
    }
}

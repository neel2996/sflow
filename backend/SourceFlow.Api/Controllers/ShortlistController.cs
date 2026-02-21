using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceFlow.Api.Data;
using SourceFlow.Api.Dtos;
using SourceFlow.Api.Models;

namespace SourceFlow.Api.Controllers;

[ApiController]
[Route("shortlist")]
[Authorize]
public class ShortlistController : ControllerBase
{
    private readonly AppDbContext _db;

    public ShortlistController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Shortlist([FromBody] ShortlistRequest req)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == req.JobId && j.UserId == userId);
        if (job == null)
            return NotFound(new { error = "Job not found" });

        var existing = await _db.ShortlistedCandidates
            .FirstOrDefaultAsync(s => s.UserId == userId && s.JobId == req.JobId && s.ProfileUrl == req.ProfileUrl);

        if (existing != null)
            return Ok(new ShortlistPostResponse { AlreadyShortlisted = true, Id = existing.Id });

        var candidate = new ShortlistedCandidate
        {
            UserId = userId,
            JobId = req.JobId,
            ProfileUrl = req.ProfileUrl,
            CandidateName = req.CandidateName ?? string.Empty,
            MatchScore = req.MatchScore,
            Summary = req.Summary ?? string.Empty,
            OutreachMessage = req.OutreachMessage ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
        };

        _db.ShortlistedCandidates.Add(candidate);
        await _db.SaveChangesAsync();

        return Ok(new ShortlistPostResponse { AlreadyShortlisted = false, Id = candidate.Id });
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetShortlist(int jobId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId);
        if (job == null)
            return NotFound(new { error = "Job not found" });

        var candidates = await _db.ShortlistedCandidates
            .Where(s => s.UserId == userId && s.JobId == jobId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ShortlistResponse
            {
                Id = s.Id,
                JobId = s.JobId,
                ProfileUrl = s.ProfileUrl,
                CandidateName = s.CandidateName,
                MatchScore = s.MatchScore,
                Summary = s.Summary,
                OutreachMessage = s.OutreachMessage,
                CreatedAt = s.CreatedAt,
            })
            .ToListAsync();

        return Ok(candidates);
    }
}

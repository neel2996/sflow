using Microsoft.EntityFrameworkCore;
using SourceFlow.Api.Data;
using SourceFlow.Api.Models;

namespace SourceFlow.Api.Services;

public class CacheService
{
    private readonly AppDbContext _db;

    public CacheService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProfileAnalysisCache?> GetCachedResult(string profileUrl, int jobId)
    {
        return await _db.ProfileAnalysisCache
            .FirstOrDefaultAsync(p => p.ProfileUrl == profileUrl && p.JobId == jobId);
    }

    public async Task SaveResult(string profileUrl, int jobId, string jsonResult, int matchScore)
    {
        _db.ProfileAnalysisCache.Add(new ProfileAnalysisCache
        {
            ProfileUrl = profileUrl,
            JobId = jobId,
            JsonResult = jsonResult,
            MatchScore = matchScore,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}

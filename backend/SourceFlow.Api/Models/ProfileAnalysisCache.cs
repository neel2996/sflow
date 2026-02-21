namespace SourceFlow.Api.Models;

public class ProfileAnalysisCache
{
    public int Id { get; set; }
    public string ProfileUrl { get; set; } = string.Empty;
    public int JobId { get; set; }
    public string JsonResult { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public DateTime CreatedAt { get; set; }

    public Job Job { get; set; } = null!;
}

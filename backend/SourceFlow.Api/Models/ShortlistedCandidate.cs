namespace SourceFlow.Api.Models;

public class ShortlistedCandidate
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int JobId { get; set; }
    public string ProfileUrl { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string OutreachMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Job Job { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SourceFlow.Api.Dtos;

public class ShortlistRequest
{
    [JsonPropertyName("job_id")]
    public int JobId { get; set; }

    [Required]
    [JsonPropertyName("profile_url")]
    public string ProfileUrl { get; set; } = string.Empty;

    [JsonPropertyName("candidate_name")]
    public string? CandidateName { get; set; }

    [JsonPropertyName("match_score")]
    public int MatchScore { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("outreach_message")]
    public string? OutreachMessage { get; set; }
}

public class ShortlistResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("job_id")]
    public int JobId { get; set; }

    [JsonPropertyName("profile_url")]
    public string ProfileUrl { get; set; } = string.Empty;

    [JsonPropertyName("candidate_name")]
    public string CandidateName { get; set; } = string.Empty;

    [JsonPropertyName("match_score")]
    public int MatchScore { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("outreach_message")]
    public string OutreachMessage { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class ShortlistPostResponse
{
    [JsonPropertyName("already_shortlisted")]
    public bool AlreadyShortlisted { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SourceFlow.Api.Dtos;

public class ScanRequest
{
    [Required]
    [JsonPropertyName("job_id")]
    public int JobId { get; set; }

    [Required]
    [JsonPropertyName("profile_url")]
    public string ProfileUrl { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("profile_text")]
    public string ProfileText { get; set; } = string.Empty;

    [JsonPropertyName("computed_experience_years")]
    public double? ComputedExperienceYears { get; set; }
}

public class ScanResult
{
    [JsonPropertyName("match_score")]
    public int MatchScore { get; set; }

    [JsonPropertyName("total_experience_years")]
    public double TotalExperienceYears { get; set; }

    [JsonPropertyName("seniority_level")]
    public string SeniorityLevel { get; set; } = string.Empty;

    [JsonPropertyName("strengths")]
    public List<string> Strengths { get; set; } = new();

    [JsonPropertyName("missing_skills")]
    public List<string> MissingSkills { get; set; } = new();

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("outreach_message")]
    public string OutreachMessage { get; set; } = string.Empty;
}

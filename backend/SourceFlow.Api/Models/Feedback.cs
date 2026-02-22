namespace SourceFlow.Api.Models;

public class Feedback
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? Email { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "feedback"; // "feedback" | "bug" | "feature"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}

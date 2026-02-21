namespace SourceFlow.Api.Models;

public class CreditTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CreditsChanged { get; set; }
    public string Type { get; set; } = string.Empty; // ManualAdd, Deduction
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}

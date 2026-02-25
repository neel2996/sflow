namespace SourceFlow.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int CreditsBalance { get; set; }
    public int? PlanId { get; set; }
    public string? RazorpayCustomerId { get; set; }
    public string? PaddleCustomerId { get; set; }
    public string Country { get; set; } = "IN";
    public DateTime CreatedAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    public ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

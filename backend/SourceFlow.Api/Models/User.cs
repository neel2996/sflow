namespace SourceFlow.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Credits { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    public ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
}

using SourceFlow.Api.Data;
using SourceFlow.Api.Models;

namespace SourceFlow.Api.Services;

public class CreditService
{
    private readonly AppDbContext _db;

    public CreditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> DeductCredit(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null || user.CreditsBalance <= 0)
            return false;

        user.CreditsBalance -= 1;

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            CreditsChanged = -1,
            Type = "Deduction",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCredits(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user?.CreditsBalance ?? 0;
    }

    public async Task<bool> AddCredits(int userId, int credits, string type = "Purchase")
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        user.CreditsBalance += credits;
        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            CreditsChanged = credits,
            Type = type,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return true;
    }
}

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
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null || user.Credits <= 0)
                return false;

            user.Credits -= 1;

            _db.CreditTransactions.Add(new CreditTransaction
            {
                UserId = userId,
                CreditsChanged = -1,
                Type = "Deduction",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> GetCredits(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user?.Credits ?? 0;
    }
}

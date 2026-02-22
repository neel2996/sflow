using Microsoft.EntityFrameworkCore;
using SourceFlow.Api.Models;

namespace SourceFlow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<ProfileAnalysisCache> ProfileAnalysisCache => Set<ProfileAnalysisCache>();
    public DbSet<ShortlistedCandidate> ShortlistedCandidates => Set<ShortlistedCandidate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<ProfileAnalysisCache>()
            .HasIndex(p => new { p.ProfileUrl, p.JobId })
            .IsUnique();

        modelBuilder.Entity<ShortlistedCandidate>()
            .HasIndex(s => new { s.UserId, s.JobId, s.ProfileUrl })
            .IsUnique();
    }
}

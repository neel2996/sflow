using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SourceFlow.Api.Data;
using SourceFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Render requires port binding
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

// Controllers & health checks
builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

// PostgreSQL — prefer DATABASE_URL (Render), else DefaultConnection
var rawConnStr = builder.Configuration["DATABASE_URL"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Set DATABASE_URL or ConnectionStrings:DefaultConnection");
var connStr = rawConnStr;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connStr,
        npgsql =>
        {
            npgsql.EnableRetryOnFailure(3);
            npgsql.CommandTimeout(30);
        }));

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Services
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<CreditService>();
builder.Services.AddScoped<CacheService>();
builder.Services.AddScoped<RazorpayService>();
builder.Services.AddScoped<PaddleService>();
builder.Services.AddHttpClient<AiService>();
builder.Services.AddHttpClient();

// CORS for Chrome extension + web
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowExtension", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseCors("AllowExtension");
app.UseAuthentication();
app.UseAuthorization();

// Health check
app.MapHealthChecks("/health");

// Landing page (Razorpay verification, product info)
app.MapGet("/", (IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.ContentRootPath, "wwwroot", "index.html");
    return File.Exists(path) ? Results.File(path, "text/html") : Results.NotFound();
});

// Payment redirect pages
app.MapGet("/success", () => Results.Content(
    "<!DOCTYPE html><html><head><title>Payment Complete</title></head><body style='font-family:sans-serif;text-align:center;padding:40px'><h1>Payment complete!</h1><p>You can close this tab and return to SourceFlow.</p></body></html>",
    "text/html"));

app.MapGet("/cancel", () => Results.Content(
    "<!DOCTYPE html><html><head><title>Cancelled</title></head><body style='font-family:sans-serif;text-align:center;padding:40px'><h1>Payment cancelled</h1><p>You can close this tab.</p></body></html>",
    "text/html"));

app.MapGet("/paywall", (IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.ContentRootPath, "wwwroot", "paywall.html");
    return File.Exists(path) ? Results.File(path, "text/html") : Results.NotFound();
});

app.MapGet("/legal", (IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.ContentRootPath, "wwwroot", "legal.html");
    return File.Exists(path) ? Results.File(path, "text/html") : Results.NotFound();
});

app.MapGet("/reset-password", (IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.ContentRootPath, "wwwroot", "reset-password.html");
    return File.Exists(path) ? Results.File(path, "text/html") : Results.NotFound();
});

app.MapGet("/verify-email", (IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.ContentRootPath, "wwwroot", "verify-email.html");
    return File.Exists(path) ? Results.File(path, "text/html") : Results.NotFound();
});

// Controllers
app.MapControllers();

// fix-migrations: remove LinkedIn migration from history, apply revert, then exit
var fixMigrations = args.Contains("fix-migrations");
if (fixMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Database.CanConnect())
        throw new InvalidOperationException("Cannot connect to database. Set ConnectionStrings:DefaultConnection or DATABASE_URL.");
    await using var conn = db.Database.GetDbConnection() as Npgsql.NpgsqlConnection;
    if (conn == null) throw new InvalidOperationException("Expected NpgsqlConnection");
    await conn.OpenAsync();

    // Remove LinkedIn migration from history (file was deleted)
    await using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"DELETE FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260227000000_LinkedInProfileAuth';";
        await cmd.ExecuteNonQueryAsync();
    }

    // Run revert schema (idempotent)
    await using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'LinkedInProfileUrl') THEN
                    DROP INDEX IF EXISTS ""IX_Users_LinkedInProfileUrl"";
                    ALTER TABLE ""Users"" DROP COLUMN ""LinkedInProfileUrl"";
                    ALTER TABLE ""Users"" DROP COLUMN ""DailyScansUsed"";
                    ALTER TABLE ""Users"" DROP COLUMN ""LastScanDate"";
                    ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Email"" text NOT NULL DEFAULT '';
                    ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PasswordHash"" text NOT NULL DEFAULT '';
                    ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PasswordResetToken"" text;
                    ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PasswordResetExpiry"" timestamp with time zone;
                    UPDATE ""Users"" SET ""Email"" = 'legacy@sourceflow.local' WHERE ""Email"" = '';
                    ALTER TABLE ""Users"" ALTER COLUMN ""Email"" DROP DEFAULT;
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");
                END IF;
            END $$;
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    // Mark revert migration as applied
    await using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES ('20260227100000_RevertLinkedInProfileAuth', '8.0.0') ON CONFLICT (""MigrationId"") DO NOTHING;";
        await cmd.ExecuteNonQueryAsync();
    }

    Console.WriteLine("fix-migrations: LinkedIn schema reverted, email/password restored.");
    return;
}


// Auto run migrations + seed plans
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (db.Database.CanConnect())
    {
        db.Database.Migrate();
        await SeedPlansAsync(db);
    }
}

app.Run();

static async Task SeedPlansAsync(AppDbContext db)
{
    if (await db.Plans.AnyAsync()) return;

    var plans = new[]
    {
        // India — credit packs (Razorpay)
        new SourceFlow.Api.Models.Plan { Name = "Starter", Price = 99, Currency = "INR", Credits = 50, BillingType = "one_time", Provider = "razorpay", PlanType = "credit_pack" },
        new SourceFlow.Api.Models.Plan { Name = "Growth", Price = 199, Currency = "INR", Credits = 150, BillingType = "one_time", Provider = "razorpay", PlanType = "credit_pack" },
        new SourceFlow.Api.Models.Plan { Name = "Pro", Price = 999, Currency = "INR", Credits = 1000, BillingType = "one_time", Provider = "razorpay", PlanType = "credit_pack" },

        // India — custom credits (price = credits * configured INR rate, set at purchase)
        new SourceFlow.Api.Models.Plan { Name = "Custom Credits", Price = 0, Currency = "INR", Credits = 0, BillingType = "one_time", Provider = "razorpay", PlanType = "custom", IsCustom = true },

        // Global — subscription (USD, Paddle)
        new SourceFlow.Api.Models.Plan { Name = "Starter", Price = 9, Currency = "USD", Credits = 200, BillingType = "subscription", Provider = "paddle", PaddlePriceId = "pri_starter", PlanType = "credit_pack" },
        new SourceFlow.Api.Models.Plan { Name = "Growth", Price = 19, Currency = "USD", Credits = 600, BillingType = "subscription", Provider = "paddle", PaddlePriceId = "pri_growth", PlanType = "credit_pack" },
        new SourceFlow.Api.Models.Plan { Name = "Pro", Price = 49, Currency = "USD", Credits = 2000, BillingType = "subscription", Provider = "paddle", PaddlePriceId = "pri_pro", PlanType = "credit_pack" },
    };

    db.Plans.AddRange(plans);
    await db.SaveChangesAsync();
}

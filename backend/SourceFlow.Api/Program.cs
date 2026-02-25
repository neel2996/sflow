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

// PostgreSQL — use DefaultConnection or DATABASE_URL (Render)
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"]?.Replace("postgres://", "postgresql://")
    ?? throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection or DATABASE_URL");
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

// Controllers
app.MapControllers();

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
        // India — one-time credit packs (Razorpay)
        new SourceFlow.Api.Models.Plan { Name = "Starter", Price = 99, Currency = "INR", Credits = 50, BillingType = "one_time", Provider = "razorpay" },
        new SourceFlow.Api.Models.Plan { Name = "Growth", Price = 199, Currency = "INR", Credits = 150, BillingType = "one_time", Provider = "razorpay" },
        new SourceFlow.Api.Models.Plan { Name = "Pro", Price = 999, Currency = "INR", Credits = 1000, BillingType = "one_time", Provider = "razorpay" },

        // Global — subscription (USD, Paddle)
        new SourceFlow.Api.Models.Plan { Name = "Starter", Price = 9, Currency = "USD", Credits = 200, BillingType = "subscription", Provider = "paddle", PaddlePriceId = "pri_starter" },
        new SourceFlow.Api.Models.Plan { Name = "Growth", Price = 19, Currency = "USD", Credits = 600, BillingType = "subscription", Provider = "paddle", PaddlePriceId = "pri_growth" },
        new SourceFlow.Api.Models.Plan { Name = "Pro", Price = 49, Currency = "USD", Credits = 2000, BillingType = "subscription", Provider = "paddle", PaddlePriceId = "pri_pro" },
    };

    db.Plans.AddRange(plans);
    await db.SaveChangesAsync();
}
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using SourceFlow.Api.Data;
using SourceFlow.Api.Models;

namespace SourceFlow.Api.Services;

public class RazorpayService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<RazorpayService> _logger;

    public RazorpayService(AppDbContext db, IConfiguration config, ILogger<RazorpayService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Razorpay order. Supports INR and USD (international cards).
    /// </summary>
    public async Task<(string OrderId, decimal Amount, string Currency, string Key)> CreateOrderAsync(int userId, int planId)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId && p.Provider == "razorpay");
        if (plan == null)
        {
            _logger.LogWarning("CreateOrder: Invalid plan {PlanId}", planId);
            throw new ArgumentException("Invalid plan");
        }

        var keyId = _config["Razorpay:KeyId"];
        var keySecret = _config["Razorpay:KeySecret"];
        if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(keySecret))
        {
            _logger.LogError("Razorpay not configured");
            throw new InvalidOperationException("Razorpay not configured");
        }

        var client = new RazorpayClient(keyId, keySecret);
        var orderRequest = new Dictionary<string, object>
        {
            { "amount", (int)(plan.Price * 100) },
            { "currency", plan.Currency },
            { "receipt", $"sf_{userId}_{planId}_{DateTime.UtcNow.Ticks}" },
            { "notes", new Dictionary<string, string>
                {
                    { "user_id", userId.ToString() },
                    { "plan_id", planId.ToString() }
                }
            }
        };

        var order = client.Order.Create(orderRequest);
        var orderJson = order.Attributes;
        var idObj = orderJson["id"];
        var orderIdStr = idObj?.ToString() ?? throw new InvalidOperationException("Failed to create order");

        _logger.LogInformation("Created Razorpay order {OrderId} for user {UserId} plan {PlanId}", (object)orderIdStr, (object)userId, (object)planId);
        return (orderIdStr, plan.Price, plan.Currency, keyId);
    }

    /// <summary>
    /// Fetches order from Razorpay API to get notes (user_id, plan_id). Used for payment.captured webhook.
    /// </summary>
    public async Task<Dictionary<string, string>?> GetOrderNotesAsync(string orderId)
    {
        var keyId = _config["Razorpay:KeyId"];
        var keySecret = _config["Razorpay:KeySecret"];
        if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(keySecret))
            return null;

        try
        {
            using var http = new HttpClient();
            var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            var res = await http.GetAsync($"https://api.razorpay.com/v1/orders/{orderId}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            if (!json.TryGetProperty("notes", out var notesEl)) return null;
            var result = new Dictionary<string, string>();
            foreach (var prop in notesEl.EnumerateObject())
                result[prop.Name] = prop.Value.GetString() ?? "";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch order {OrderId}", orderId);
            return null;
        }
    }

    public bool ValidateWebhookSignature(string payload, string signature)
    {
        var secret = _config["Razorpay:WebhookSecret"];
        if (string.IsNullOrEmpty(secret)) return false;

        try
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
            var computed = Convert.ToHexString(hash).ToLowerInvariant();
            return string.Equals(computed, signature, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

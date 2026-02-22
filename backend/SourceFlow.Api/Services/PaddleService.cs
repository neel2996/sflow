using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SourceFlow.Api.Data;
using SourceFlow.Api.Models;

namespace SourceFlow.Api.Services;

/// <summary>
/// Paddle Billing service for USD/international subscription payments.
/// Creates checkout URLs and verifies webhooks.
/// </summary>
public class PaddleService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<PaddleService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public PaddleService(AppDbContext db, IConfiguration config, IHttpClientFactory httpFactory, ILogger<PaddleService> logger)
    {
        _db = db;
        _config = config;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    private string ApiBase => _config.GetValue<bool>("Paddle:Sandbox")
        ? "https://sandbox-api.paddle.com"
        : "https://api.paddle.com";

    /// <summary>
    /// Creates a Paddle checkout transaction and returns the checkout URL.
    /// </summary>
    public async Task<string> CreateCheckoutUrlAsync(int userId, int planId, string customerEmail, string? returnUrl = null)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId && p.Provider == "paddle");
        if (plan == null)
        {
            _logger.LogWarning("CreateCheckoutUrl: Invalid plan {PlanId}", planId);
            throw new ArgumentException("Invalid plan");
        }

        var priceId = plan.PaddlePriceId;
        if (string.IsNullOrEmpty(priceId))
        {
            _logger.LogError("Plan {PlanId} has no PaddlePriceId", planId);
            throw new InvalidOperationException("International payments not accepted yet.");
        }

        var apiKey = _config["Paddle:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("Paddle API key not configured");
            throw new InvalidOperationException("International payments not accepted yet.");
        }

        var user = await _db.Users.FindAsync(userId);
        if (user == null) throw new ArgumentException("User not found");

        // Build transaction request
        var items = new[] { new { price_id = priceId, quantity = 1 } };
        var customData = new Dictionary<string, string>
        {
            { "user_id", userId.ToString() },
            { "plan_id", planId.ToString() }
        };

        var body = new Dictionary<string, object>
        {
            ["items"] = items,
            ["custom_data"] = customData,
            ["currency_code"] = plan.Currency,
            ["collection_mode"] = "automatic"
        };

        var http = _httpFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/transactions");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        var response = await http.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Paddle create transaction failed: {Status} {Body}", response.StatusCode, responseText);
            throw new InvalidOperationException($"Paddle checkout failed: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseText);
        var data = doc.RootElement.GetProperty("data");
        var checkout = data.TryGetProperty("checkout", out var c) ? c : default;
        var checkoutUrl = checkout.ValueKind == JsonValueKind.Object && checkout.TryGetProperty("url", out var urlEl)
            ? urlEl.GetString()
            : null;

        if (string.IsNullOrEmpty(checkoutUrl))
        {
            var txnId = data.TryGetProperty("id", out var idEl) ? idEl.GetString() : "unknown";
            checkoutUrl = $"{ApiBase.Replace("-api", "")}/checkout/custom?transaction_id={txnId}";
        }

        var txnIdStr = data.TryGetProperty("id", out var idE) ? idE.GetString() : "";
        _logger.LogInformation("Created Paddle checkout for user {UserId} plan {PlanId} txn {TxnId}", userId, planId, txnIdStr);

        return checkoutUrl;
    }

    /// <summary>
    /// Verifies Paddle webhook signature. See https://developer.paddle.com/webhooks/overview.
    /// </summary>
    public bool ValidateWebhookSignature(string payload, string signature)
    {
        var secret = _config["Paddle:WebhookSecret"];
        if (string.IsNullOrEmpty(secret)) return false;

        try
        {
            var parts = signature.Split(';');
            string? ts = null, h1 = null;
            foreach (var p in parts)
            {
                var kv = p.Split('=', 2, StringSplitOptions.None);
                if (kv.Length != 2) continue;
                if (kv[0].Trim() == "ts") ts = kv[1].Trim();
                else if (kv[0].Trim() == "h1") h1 = kv[1].Trim();
            }
            if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(h1)) return false;

            var signedPayload = $"{ts}:{payload}";
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
            var computed = Convert.ToHexString(hash).ToLowerInvariant();
            return string.Equals(computed, h1, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

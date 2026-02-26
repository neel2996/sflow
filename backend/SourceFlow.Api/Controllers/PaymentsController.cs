using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceFlow.Api.Data;
using SourceFlow.Api.Dtos;
using SourceFlow.Api.Models;
using SourceFlow.Api.Services;

namespace SourceFlow.Api.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly RazorpayService _razorpay;
    private readonly PaddleService _paddle;
    private readonly CreditService _credits;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(AppDbContext db, RazorpayService razorpay, PaddleService paddle, CreditService credits, ILogger<PaymentsController> logger)
    {
        _db = db;
        _razorpay = razorpay;
        _paddle = paddle;
        _credits = credits;
        _logger = logger;
    }

    [HttpGet("client-config")]
    [AllowAnonymous]
    public IActionResult GetClientConfig()
    {
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var razorpayKey = config["Razorpay:KeyId"];
        var paddleEnabled = !string.IsNullOrEmpty(config["Paddle:ApiKey"]);
        var mockEnabled = config.GetValue<bool>("Razorpay:EnableMockPayments")
            || HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
        return Ok(new { razorpay_key_id = razorpayKey ?? "", paddle_enabled = paddleEnabled, mock_enabled = mockEnabled });
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans([FromQuery] string country = "IN")
    {
        try
        {
            var isIndia = string.Equals(country, "IN", StringComparison.OrdinalIgnoreCase);
            var currency = isIndia ? "INR" : "USD";

            // INR -> razorpay, USD -> paddle
            var provider = currency == "INR" ? "razorpay" : "paddle";
            var plans = await _db.Plans
                .Where(p => p.Provider.ToLower() == provider && p.Currency == currency)
                .Select(p => new PlanResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Currency = p.Currency,
                    Credits = p.Credits,
                    BillingType = p.BillingType,
                    Provider = p.Provider,
                    PlanType = p.PlanType ?? "credit_pack",
                    DurationHours = p.DurationHours,
                    IsCustom = p.IsCustom
                })
                .ToListAsync();

            // Order: Starter, Growth, Pro first (by price), then Custom Credits last
            var isCustom = (PlanResponse p) => (p.PlanType ?? "").ToLower() == "custom" || p.IsCustom || (p.Price == 0 && p.Credits == 0 && (p.Name ?? "").Contains("Custom", StringComparison.OrdinalIgnoreCase));
            return Ok(plans
                .OrderBy(p => isCustom(p) ? 1 : 0)
                .ThenBy(p => p.Price)
                .ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPlans failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /payments/create-order — Creates Razorpay order (INR) or Paddle checkout (USD).
    /// </summary>
    [HttpPost("create-order")]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var plan = await _db.Plans.FindAsync(req.PlanId);
        if (plan == null) return BadRequest(new { error = "Invalid plan" });

        // Custom plan: require credits, price = credits * 1 INR
        if (plan.IsCustom || (plan.PlanType ?? "").ToLower() == "custom")
        {
            var credits = req.Credits ?? 0;
            if (credits < 1 || credits > 10000)
                return BadRequest(new { error = "Credits must be between 1 and 10000" });
            var amount = credits * 1m; // 1 INR per credit

            if (!string.Equals(user.Country, "IN", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Custom credits available for India only." });

            var (orderId, _, _, key) = await _razorpay.CreateOrderWithAmountAsync(userId, req.PlanId, amount, credits);
            return Ok(new CreateOrderResponse
            {
                Provider = "razorpay",
                OrderId = orderId,
                Amount = amount,
                Currency = "INR",
                Key = key
            });
        }

        var isIndia = string.Equals(user.Country, "IN", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(plan.Provider, "razorpay", StringComparison.OrdinalIgnoreCase) && !isIndia)
            return BadRequest(new { error = "International payments are temporarily unavailable." });
        if (string.Equals(plan.Provider, "paddle", StringComparison.OrdinalIgnoreCase) && isIndia)
            return BadRequest(new { error = "Indian users must use INR plans. Switch to India (IN) or use the INR pricing." });

        if (string.Equals(plan.Provider, "paddle", StringComparison.OrdinalIgnoreCase))
        {
            var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            if (string.IsNullOrEmpty(config["Paddle:ApiKey"]))
                return BadRequest(new { error = "International payments not accepted yet." });
        }

        try
        {
            if (string.Equals(plan.Provider, "razorpay", StringComparison.OrdinalIgnoreCase))
            {
                var (orderId, amount, currency, key) = await _razorpay.CreateOrderAsync(userId, req.PlanId);
                return Ok(new CreateOrderResponse
                {
                    Provider = "razorpay",
                    OrderId = orderId,
                    Amount = amount,
                    Currency = currency,
                    Key = key
                });
            }

            if (string.Equals(plan.Provider, "paddle", StringComparison.OrdinalIgnoreCase))
            {
                var returnUrl = $"{Request.Scheme}://{Request.Host}/success";
                var checkoutUrl = await _paddle.CreateCheckoutUrlAsync(userId, req.PlanId, user.Email, returnUrl);
                return Ok(new CreateOrderResponse
                {
                    Provider = "paddle",
                    CheckoutUrl = checkoutUrl,
                    Amount = plan.Price,
                    Currency = plan.Currency
                });
            }

            return BadRequest(new { error = "Unsupported payment provider" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Legacy endpoint — use create-order. Kept for extension compatibility. Only for Razorpay plans.
    /// </summary>
    [HttpPost("create-razorpay-order")]
    [Authorize]
    public async Task<IActionResult> CreateRazorpayOrder([FromBody] CreateRazorpayOrderRequest req)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        var isIndia = string.Equals(user.Country, "IN", StringComparison.OrdinalIgnoreCase);
        if (!isIndia)
            return BadRequest(new { error = "International payments are temporarily unavailable." });

        var plan = await _db.Plans.FindAsync(req.PlanId);
        if (plan == null || !string.Equals(plan.Provider, "razorpay", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Invalid or non-Razorpay plan" });
        try
        {
            var (orderId, _, _, _) = await _razorpay.CreateOrderAsync(userId, req.PlanId);
            return Ok(new { order_id = orderId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("mock-razorpay-success")]
    [Authorize]
    public async Task<IActionResult> MockRazorpaySuccess([FromBody] CreateOrderRequest req)
    {
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (!config.GetValue<bool>("Razorpay:EnableMockPayments") && !env.IsDevelopment())
            return NotFound();

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        var isIndia = string.Equals(user.Country, "IN", StringComparison.OrdinalIgnoreCase);
        if (!isIndia)
            return BadRequest(new { error = "International payments are temporarily unavailable." });
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == req.PlanId && p.Provider == "razorpay");
        if (plan == null) return BadRequest(new { error = "Invalid plan" });

        int? creditsOverride = null;
        if (plan.IsCustom || (plan.PlanType ?? "").ToLower() == "custom")
        {
            var credits = req.Credits ?? 0;
            if (credits < 1 || credits > 10000)
                return BadRequest(new { error = "Credits must be between 1 and 10000 for custom plan" });
            creditsOverride = credits;
        }

        await AddCreditsAndRecordPayment(userId, plan, "mock", creditsOverride: creditsOverride);
        var planType = (plan.PlanType ?? "").ToLower();
        var creditsAdded = planType == "unlimited" ? 0 : (creditsOverride ?? plan.Credits);
        var msg = planType == "unlimited" ? "Unlimited access activated" : $"Credits added: {creditsAdded}";
        return Ok(new { credits_added = creditsAdded, message = msg });
    }

    [HttpPost("razorpay-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> RazorpayWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature) || !_razorpay.ValidateWebhookSignature(json, signature))
        {
            _logger.LogWarning("Razorpay webhook: Invalid signature");
            return BadRequest(new { error = "Invalid signature" });
        }

        var evt = JsonSerializer.Deserialize<RazorpayWebhookPayload>(json);
        if (evt == null) return Ok();

        // Handle order.paid (has order with notes) and payment.captured (has payment with order_id)
        string? orderId = null;
        string? paymentId = null;
        Dictionary<string, string>? notes = null;

        if (evt.Event == "order.paid" && evt.Payload?.Order?.Entity != null)
        {
            orderId = evt.Payload.Order.Entity.Id;
            notes = ParseNotes(evt.Payload.Order.Entity.NotesRaw);
        }
        else if (evt.Event == "payment.captured" && evt.Payload?.Payment?.Entity != null)
        {
            paymentId = evt.Payload.Payment.Entity.Id;
            orderId = evt.Payload.Payment.Entity.OrderId;
            if (!string.IsNullOrEmpty(orderId))
                notes = await _razorpay.GetOrderNotesAsync(orderId);
        }

        if (notes == null || !notes.TryGetValue("user_id", out var userIdStr) || !notes.TryGetValue("plan_id", out var planIdStr)
            || string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(planIdStr)
            || !int.TryParse(userIdStr, out var userId) || !int.TryParse(planIdStr, out var planId))
        {
            _logger.LogInformation("Razorpay webhook {Event}: Ignored (no user/plan)", evt.Event);
            return Ok();
        }

        // Idempotency: prevent duplicate credits
        var existing = await _db.Payments
            .FirstOrDefaultAsync(p => (p.ExternalPaymentId == orderId || p.RazorpayOrderId == orderId) && p.Status == "Completed");
        if (existing != null)
        {
            _logger.LogInformation("Razorpay webhook: Order {OrderId} already processed", orderId);
            return Ok();
        }

        var plan = await _db.Plans.FindAsync(planId);
        var user = await _db.Users.FindAsync(userId);
        if (plan == null || user == null)
        {
            _logger.LogWarning("Razorpay webhook: Plan {PlanId} or User {UserId} not found", planId, userId);
            return Ok();
        }

        int? creditsOverride = null;
        decimal? amountOverride = null;
        if (plan.IsCustom || (plan.PlanType ?? "").ToLower() == "custom")
        {
            if (notes.TryGetValue("credits", out var creditsStr) && int.TryParse(creditsStr, out var c) && c > 0)
            {
                creditsOverride = c;
                amountOverride = c * 1m; // 1 INR per credit
            }
        }

        try
        {
            await AddCreditsAndRecordPayment(userId, plan, "razorpay", externalId: orderId, razorpayOrderId: orderId, razorpayPaymentId: paymentId, creditsOverride: creditsOverride, amountOverride: amountOverride);
            _logger.LogInformation("Razorpay webhook: Processed order {OrderId} for user {UserId}", orderId, userId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Razorpay webhook: Failed to process order {OrderId}", orderId);
            throw;
        }
    }

    [HttpPost("paddle-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PaddleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Paddle-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature) || !_paddle.ValidateWebhookSignature(json, signature))
        {
            _logger.LogWarning("Paddle webhook: Invalid signature");
            return BadRequest(new { error = "Invalid signature" });
        }

        var evt = JsonSerializer.Deserialize<PaddleWebhookPayload>(json);
        if (evt == null || evt.Data == null) return Ok();

        if (evt.EventType != "transaction.completed") return Ok();

        var txnId = evt.Data.Id;
        var customData = evt.Data.CustomData;
        if (customData == null || !customData.TryGetValue("user_id", out var userIdStr) || !customData.TryGetValue("plan_id", out var planIdStr)
            || string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(planIdStr)
            || !int.TryParse(userIdStr, out var userId) || !int.TryParse(planIdStr, out var planId))
        {
            _logger.LogInformation("Paddle webhook: Ignored (no user/plan in custom_data)");
            return Ok();
        }

        var existing = await _db.Payments
            .FirstOrDefaultAsync(p => p.ExternalPaymentId == txnId && p.Status == "Completed");
        if (existing != null)
        {
            _logger.LogInformation("Paddle webhook: Transaction {TxnId} already processed", txnId);
            return Ok();
        }

        var plan = await _db.Plans.FindAsync(planId);
        var user = await _db.Users.FindAsync(userId);
        if (plan == null || user == null)
        {
            _logger.LogWarning("Paddle webhook: Plan {PlanId} or User {UserId} not found", planId, userId);
            return Ok();
        }

        try
        {
            await AddCreditsAndRecordPayment(userId, plan, "paddle", externalId: txnId);
            _logger.LogInformation("Paddle webhook: Credited user {UserId} with {Credits} credits", userId, plan.Credits);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paddle webhook: Failed to process transaction {TxnId}", txnId);
            throw;
        }
    }

    /// <summary>
    /// Verify Razorpay payment after checkout (production). Adds credits if not already processed by webhook.
    /// </summary>
    [HttpPost("verify-razorpay")]
    [Authorize]
    public async Task<IActionResult> VerifyRazorpayPayment([FromBody] VerifyRazorpayRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.OrderId) || string.IsNullOrWhiteSpace(req.PaymentId))
            return BadRequest(new { error = "order_id and payment_id required" });

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var existing = await _db.Payments
            .FirstOrDefaultAsync(p => (p.ExternalPaymentId == req.OrderId || p.RazorpayOrderId == req.OrderId) && p.Status == "Completed");
        if (existing != null)
        {
            var balance = (await _db.Users.FindAsync(userId))?.CreditsBalance ?? 0;
            return Ok(new { credits_added = 0, new_balance = balance, message = "Already verified" });
        }

        var notes = await _razorpay.GetOrderNotesAsync(req.OrderId);
        if (notes == null || !notes.TryGetValue("user_id", out var uid) || !notes.TryGetValue("plan_id", out var pid)
            || uid != userId.ToString() || !int.TryParse(pid, out var planId))
        {
            _logger.LogWarning("VerifyRazorpay: Order {OrderId} notes mismatch or invalid", req.OrderId);
            return BadRequest(new { error = "Invalid order" });
        }

        var plan = await _db.Plans.FindAsync(planId);
        if (plan == null || plan.Provider?.ToLower() != "razorpay")
            return BadRequest(new { error = "Invalid plan" });

        int? creditsOverride = null;
        decimal? amountOverride = null;
        if (plan.IsCustom || (plan.PlanType ?? "").ToLower() == "custom")
        {
            if (notes.TryGetValue("credits", out var creditsStr) && int.TryParse(creditsStr, out var c) && c > 0)
            {
                creditsOverride = c;
                amountOverride = c * 1m;
            }
        }

        await AddCreditsAndRecordPayment(userId, plan, "razorpay", externalId: req.OrderId, razorpayOrderId: req.OrderId, razorpayPaymentId: req.PaymentId, creditsOverride: creditsOverride, amountOverride: amountOverride);
        var freshUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var creditsAdded = (plan.PlanType ?? "").ToLower() == "unlimited" ? 0 : (creditsOverride ?? plan.Credits);
        return Ok(new { credits_added = creditsAdded, new_balance = freshUser?.CreditsBalance ?? 0, unlimited_till = freshUser?.UnlimitedAccessTill, message = creditsAdded > 0 ? "Credits added" : "Unlimited access activated" });
    }

    [HttpPost("simulate-razorpay-webhook")]
    [Authorize]
    public async Task<IActionResult> SimulateRazorpayWebhook([FromBody] SimulateWebhookRequest req)
    {
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (!config.GetValue<bool>("Razorpay:EnableMockPayments") && !env.IsDevelopment())
            return NotFound();

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        if (!string.Equals(user.Country, "IN", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "International payments are temporarily unavailable." });
        // Use client-provided payment_id for idempotency so each purchase gets unique credits
        var orderId = !string.IsNullOrWhiteSpace(req.PaymentId) ? req.PaymentId : $"sim_{userId}_{req.PlanId}";

        // Idempotency: don't add credits if already verified for this order
        var existing = await _db.Payments
            .FirstOrDefaultAsync(p => (p.ExternalPaymentId == orderId || p.RazorpayOrderId == orderId) && p.Status == "Completed");
        if (existing != null)
        {
            return Ok(new { credits_added = 0, new_balance = user.CreditsBalance, message = "Already verified" });
        }

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == req.PlanId && (p.Provider == "razorpay" || p.Provider == "Razorpay"));
        if (plan == null) return BadRequest(new { error = "Invalid plan" });

        int? creditsOverride = null;
        if (plan.IsCustom || (plan.PlanType ?? "").ToLower() == "custom")
        {
            var credits = req.Credits ?? 0;
            if (credits < 1 || credits > 10000)
                return BadRequest(new { error = "Credits must be between 1 and 10000 for custom plan" });
            creditsOverride = credits;
        }

        var userEntity = await _db.Users.FindAsync(userId);
        if (userEntity == null) return NotFound();

        await AddCreditsAndRecordPayment(userId, plan, "razorpay", externalId: orderId, razorpayOrderId: orderId, razorpayPaymentId: null, creditsOverride: creditsOverride, amountOverride: creditsOverride.HasValue ? creditsOverride.Value * 1m : null);

        var freshUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var creditsAdded = (plan.PlanType ?? "").ToLower() == "unlimited" ? 0 : (creditsOverride ?? plan.Credits);
        return Ok(new { credits_added = creditsAdded, new_balance = freshUser?.CreditsBalance ?? 0, unlimited_till = freshUser?.UnlimitedAccessTill, message = creditsAdded > 0 ? "Webhook simulated" : "Unlimited access activated" });
    }

    private async Task AddCreditsAndRecordPayment(int userId, Plan plan, string provider,
        string? externalId = null, string? razorpayOrderId = null, string? razorpayPaymentId = null, int? creditsOverride = null, decimal? amountOverride = null)
    {
        var planType = (plan.PlanType ?? "credit_pack").ToLower();
        var amount = amountOverride ?? plan.Price;

        if (planType == "unlimited" && plan.DurationHours.HasValue && plan.DurationHours > 0)
        {
            await _credits.SetUnlimitedAccess(userId, plan.DurationHours.Value);
        }
        else
        {
            var credits = creditsOverride ?? plan.Credits;
            if (credits > 0)
            {
                var creditType = string.Equals(plan.BillingType, "subscription", StringComparison.OrdinalIgnoreCase)
                    ? "Subscription"
                    : "Purchase";
                await _credits.AddCredits(userId, credits, creditType);
            }
        }

        _db.Payments.Add(new Payment
        {
            UserId = userId,
            PlanId = plan.Id,
            Amount = amount,
            Currency = plan.Currency,
            Provider = provider,
            ExternalPaymentId = externalId ?? razorpayOrderId,
            RazorpayOrderId = razorpayOrderId,
            RazorpayPaymentId = razorpayPaymentId,
            Status = "Completed",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private class RazorpayWebhookPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("event")]
        public string? Event { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("payload")]
        public RazorpayPayload? Payload { get; set; }
    }

    private class RazorpayPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("order")]
        public RazorpayOrderWrapper? Order { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("payment")]
        public RazorpayPaymentWrapper? Payment { get; set; }
    }

    private class RazorpayOrderWrapper
    {
        [System.Text.Json.Serialization.JsonPropertyName("entity")]
        public RazorpayOrderEntity? Entity { get; set; }
    }

    private class RazorpayOrderEntity
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("notes")]
        public JsonElement NotesRaw { get; set; }
    }

    private static Dictionary<string, string>? ParseNotes(JsonElement notesEl)
    {
        if (notesEl.ValueKind != JsonValueKind.Object) return null;
        var d = new Dictionary<string, string>();
        foreach (var p in notesEl.EnumerateObject())
            d[p.Name] = p.Value.GetString() ?? string.Empty;
        return d;
    }

    private class RazorpayPaymentWrapper
    {
        [System.Text.Json.Serialization.JsonPropertyName("entity")]
        public RazorpayPaymentEntity? Entity { get; set; }
    }

    private class RazorpayPaymentEntity
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("order_id")]
        public string? OrderId { get; set; }
    }

    private class PaddleWebhookPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("event_type")]
        public string? EventType { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public PaddleWebhookData? Data { get; set; }
    }

    private class PaddleWebhookData
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("custom_data")]
        public Dictionary<string, string>? CustomData { get; set; }
    }
}

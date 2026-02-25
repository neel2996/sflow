using System.Text.Json.Serialization;

namespace SourceFlow.Api.Dtos;

public class CreateOrderRequest
{
    [JsonPropertyName("plan_id")]
    public int PlanId { get; set; }
}

public class CreateOrderResponse
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "razorpay";

    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>For Paddle: redirect URL to checkout.</summary>
    [JsonPropertyName("checkout_url")]
    public string? CheckoutUrl { get; set; }
}

public class CreateRazorpayOrderRequest
{
    [JsonPropertyName("plan_id")]
    public int PlanId { get; set; }
}

public class SimulateWebhookRequest
{
    [JsonPropertyName("plan_id")]
    public int PlanId { get; set; }

    [JsonPropertyName("payment_id")]
    public string? PaymentId { get; set; }
}

public class VerifyRazorpayRequest
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; } = string.Empty;
}

public class PlanResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("credits")]
    public int Credits { get; set; }

    [JsonPropertyName("billing_type")]
    public string BillingType { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;
}

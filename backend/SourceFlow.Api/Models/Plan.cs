namespace SourceFlow.Api.Models;

public class Plan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int Credits { get; set; }
    public string BillingType { get; set; } = "OneTime";
    public string Provider { get; set; } = "razorpay";
    /// <summary>Paddle price ID (pri_xxx) for Paddle-provider plans.</summary>
    public string? PaddlePriceId { get; set; }
    /// <summary>credit_pack | unlimited | custom</summary>
    public string PlanType { get; set; } = "credit_pack";
    /// <summary>For unlimited plans: duration in hours.</summary>
    public int? DurationHours { get; set; }
    /// <summary>True for custom credit purchase (price = credits * 1 INR).</summary>
    public bool IsCustom { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

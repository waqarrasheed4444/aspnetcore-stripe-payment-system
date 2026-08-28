using CleanArchitecture.Application.Common.Models;

namespace CleanArchitecture.Application.Common.Interfaces;

public class CreateCheckoutSessionModel
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int InternalPaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string Description { get; set; } = string.Empty;
    public List<CheckoutItemModel> Items { get; set; } = new();
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class CheckoutItemModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitAmount { get; set; }
    public int Quantity { get; set; }
}

public class CheckoutSessionResult
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionUrl { get; set; } = string.Empty;
    public string? PaymentIntentId { get; set; }
    public string? CustomerId { get; set; }
}

public class RefundResult
{
    public string RefundId { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public string ChargeId { get; set; } = string.Empty;
    public decimal AmountRefunded { get; set; }
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public interface IStripePaymentService
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionModel model, CancellationToken cancellationToken = default);
    Task<RefundResult> CreateRefundAsync(string paymentIntentId, decimal amount, string currency, string? reason = null, CancellationToken cancellationToken = default);
    Task<string> GetOrCreateCustomerAsync(string userId, string email, string name, CancellationToken cancellationToken = default);
    Task<StripeWebhookEventResult> ParseAndValidateWebhookAsync(string jsonPayload, string stripeSignatureHeader, CancellationToken cancellationToken = default);
}

public class StripeWebhookEventResult
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public object? DataObject { get; set; }
    public string? RawJson { get; set; }
}

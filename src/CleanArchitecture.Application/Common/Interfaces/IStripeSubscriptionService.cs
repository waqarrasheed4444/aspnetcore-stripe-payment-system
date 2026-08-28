namespace CleanArchitecture.Application.Common.Interfaces;

public class CreateSubscriptionCheckoutModel
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string PriceId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int InternalSubscriptionId { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class SubscriptionCancellationResult
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
}

public interface IStripeSubscriptionService
{
    Task<CheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(CreateSubscriptionCheckoutModel model, CancellationToken cancellationToken = default);
    Task<string> CreateCustomerPortalSessionAsync(string stripeCustomerId, string returnUrl, CancellationToken cancellationToken = default);
    Task<SubscriptionCancellationResult> CancelSubscriptionAsync(string stripeSubscriptionId, bool cancelImmediately = false, CancellationToken cancellationToken = default);
}

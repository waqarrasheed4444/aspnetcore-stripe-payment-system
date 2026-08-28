namespace CleanArchitecture.Infrastructure.Stripe;

public class StripeSettings
{
    public const string SectionName = "Stripe";

    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = "https://localhost:5001/checkout/success?session_id={CHECKOUT_SESSION_ID}";
    public string CancelUrl { get; set; } = "https://localhost:5001/checkout/cancel";
    public string PortalReturnUrl { get; set; } = "https://localhost:5001/account/billing";
}

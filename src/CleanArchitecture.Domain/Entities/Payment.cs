using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class Payment : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public string StripeCustomerId { get; set; } = string.Empty;
    public string? StripePaymentIntentId { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    public decimal Amount { get; set; }
    public decimal AmountRefunded { get; set; }
    public string Currency { get; set; } = "usd";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? Description { get; set; }
    public string? FailureMessage { get; set; }

    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}

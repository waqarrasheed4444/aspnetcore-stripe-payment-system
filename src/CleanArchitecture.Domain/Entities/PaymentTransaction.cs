using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class PaymentTransaction : AuditableEntity
{
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public string? StripeChargeId { get; set; }
    public string? StripeRefundId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public PaymentTransactionType Type { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}

using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Payments.Dtos;

public class PaymentDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public string StripeCustomerId { get; set; } = string.Empty;
    public string? StripePaymentIntentId { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountRefunded { get; set; }
    public string Currency { get; set; } = "usd";
    public PaymentStatus Status { get; set; }
    public string? Description { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public List<PaymentTransactionDto> Transactions { get; set; } = new();
}

public class PaymentTransactionDto
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public string? StripeChargeId { get; set; }
    public string? StripeRefundId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public PaymentTransactionType Type { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CheckoutSessionResponseDto
{
    public int PaymentId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
}

public class RefundPaymentResponseDto
{
    public int PaymentId { get; set; }
    public string RefundId { get; set; } = string.Empty;
    public decimal AmountRefunded { get; set; }
    public decimal TotalAmountRefunded { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string Status { get; set; } = string.Empty;
}

using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Payments.Dtos;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Payments.Commands.RefundPayment;

public class RefundPaymentCommand : IRequest<RefundPaymentResponseDto>
{
    public int PaymentId { get; set; }
    public decimal? Amount { get; set; }
    public string? Reason { get; set; }
}

public class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId).GreaterThan(0).WithMessage("Valid PaymentId is required.");
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Amount.HasValue)
            .WithMessage("Refund amount must be greater than zero.");
    }
}

public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, RefundPaymentResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IStripePaymentService _stripePaymentService;

    public RefundPaymentCommandHandler(
        IApplicationDbContext context,
        IStripePaymentService stripePaymentService)
    {
        _context = context;
        _stripePaymentService = stripePaymentService;
    }

    public async Task<RefundPaymentResponseDto> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment == null)
        {
            throw new NotFoundException($"Payment with ID {request.PaymentId} was not found.");
        }

        if (payment.Status != PaymentStatus.Paid && payment.Status != PaymentStatus.PartiallyRefunded)
        {
            throw new PaymentException($"Only paid or partially refunded payments can be refunded. Current status is '{payment.Status}'.");
        }

        if (string.IsNullOrEmpty(payment.StripePaymentIntentId))
        {
            throw new PaymentException("Payment does not have an associated Stripe PaymentIntent ID.");
        }

        decimal maxRefundable = payment.Amount - payment.AmountRefunded;
        if (maxRefundable <= 0)
        {
            throw new PaymentException("Payment has already been fully refunded.");
        }

        decimal refundAmount = request.Amount ?? maxRefundable;
        if (refundAmount > maxRefundable)
        {
            throw new PaymentException($"Refund amount of {refundAmount:C} exceeds maximum refundable amount of {maxRefundable:C}.");
        }

        // Call Stripe Refund API
        var refundResult = await _stripePaymentService.CreateRefundAsync(
            payment.StripePaymentIntentId,
            refundAmount,
            payment.Currency,
            request.Reason,
            cancellationToken);

        // Update local ledger
        payment.AmountRefunded += refundAmount;
        payment.Status = payment.AmountRefunded >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;

        var transaction = new PaymentTransaction
        {
            PaymentId = payment.Id,
            StripeRefundId = refundResult.RefundId,
            StripeChargeId = refundResult.ChargeId,
            Amount = refundAmount,
            Currency = payment.Currency,
            Type = PaymentTransactionType.Refund,
            Status = refundResult.Status,
            FailureReason = null
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return new RefundPaymentResponseDto
        {
            PaymentId = payment.Id,
            RefundId = refundResult.RefundId,
            AmountRefunded = refundAmount,
            TotalAmountRefunded = payment.AmountRefunded,
            PaymentStatus = payment.Status,
            Status = refundResult.Status
        };
    }
}

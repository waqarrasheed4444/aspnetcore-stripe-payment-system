using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Payments.Queries.GetPaymentsByUserId;

public record GetPaymentsByUserIdQuery(string UserId) : IRequest<List<PaymentDto>>;

public class GetPaymentsByUserIdQueryHandler : IRequestHandler<GetPaymentsByUserIdQuery, List<PaymentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPaymentsByUserIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PaymentDto>> Handle(GetPaymentsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var payments = await _context.Payments
            .Include(p => p.Transactions)
            .Where(p => p.UserId == request.UserId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(payment => new PaymentDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            OrderId = payment.OrderId,
            StripeCustomerId = payment.StripeCustomerId,
            StripePaymentIntentId = payment.StripePaymentIntentId,
            StripeCheckoutSessionId = payment.StripeCheckoutSessionId,
            StripeSubscriptionId = payment.StripeSubscriptionId,
            Amount = payment.Amount,
            AmountRefunded = payment.AmountRefunded,
            Currency = payment.Currency,
            Status = payment.Status,
            Description = payment.Description,
            FailureMessage = payment.FailureMessage,
            CreatedAt = payment.CreatedAt,
            LastModifiedAt = payment.LastModifiedAt,
            Transactions = payment.Transactions.Select(t => new PaymentTransactionDto
            {
                Id = t.Id,
                PaymentId = t.PaymentId,
                StripeChargeId = t.StripeChargeId,
                StripeRefundId = t.StripeRefundId,
                Amount = t.Amount,
                Currency = t.Currency,
                Type = t.Type,
                Status = t.Status,
                FailureReason = t.FailureReason,
                CreatedAt = t.CreatedAt
            }).ToList()
        }).ToList();
    }
}

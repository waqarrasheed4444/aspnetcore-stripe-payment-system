using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Payments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Payments.Queries.GetPaymentById;

public record GetPaymentByIdQuery(int Id) : IRequest<PaymentDto>;

public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, PaymentDto>
{
    private readonly IApplicationDbContext _context;

    public GetPaymentByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentDto> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (payment == null)
        {
            throw new NotFoundException($"Payment with ID {request.Id} was not found.");
        }

        return new PaymentDto
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
        };
    }
}

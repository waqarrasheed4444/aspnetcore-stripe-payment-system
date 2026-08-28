using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Subscriptions.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Subscriptions.Queries.GetSubscriptionByUserId;

public record GetSubscriptionByUserIdQuery(string UserId) : IRequest<SubscriptionDto?>;

public class GetSubscriptionByUserIdQueryHandler : IRequestHandler<GetSubscriptionByUserIdQuery, SubscriptionDto?>
{
    private readonly IApplicationDbContext _context;

    public GetSubscriptionByUserIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionDto?> Handle(GetSubscriptionByUserIdQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _context.Subscriptions
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
            return null;

        return new SubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            StripeCustomerId = subscription.StripeCustomerId,
            StripeSubscriptionId = subscription.StripeSubscriptionId,
            StripePriceId = subscription.StripePriceId,
            PlanName = subscription.PlanName,
            Status = subscription.Status,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
            CanceledAt = subscription.CanceledAt,
            CreatedAt = subscription.CreatedAt
        };
    }
}

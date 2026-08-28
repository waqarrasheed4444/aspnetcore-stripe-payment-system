using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Subscriptions.Dtos;
using CleanArchitecture.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Subscriptions.Commands.CancelSubscription;

public class CancelSubscriptionCommand : IRequest<SubscriptionDto>
{
    public int SubscriptionId { get; set; }
    public bool CancelImmediately { get; set; } = false;
}

public class CancelSubscriptionCommandValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).GreaterThan(0).WithMessage("Valid SubscriptionId is required.");
    }
}

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, SubscriptionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IStripeSubscriptionService _stripeSubscriptionService;

    public CancelSubscriptionCommandHandler(
        IApplicationDbContext context,
        IStripeSubscriptionService stripeSubscriptionService)
    {
        _context = context;
        _stripeSubscriptionService = stripeSubscriptionService;
    }

    public async Task<SubscriptionDto> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken);
        if (subscription == null)
        {
            throw new NotFoundException($"Subscription with ID {request.SubscriptionId} was not found.");
        }

        if (string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            throw new PaymentException("Subscription does not have an active Stripe Subscription ID.");
        }

        if (subscription.Status == SubscriptionStatus.Canceled)
        {
            throw new PaymentException("Subscription is already canceled.");
        }

        var result = await _stripeSubscriptionService.CancelSubscriptionAsync(
            subscription.StripeSubscriptionId,
            request.CancelImmediately,
            cancellationToken);

        subscription.CancelAtPeriodEnd = result.CancelAtPeriodEnd;
        if (request.CancelImmediately || result.Status == "canceled")
        {
            subscription.Status = SubscriptionStatus.Canceled;
            subscription.CanceledAt = result.CanceledAt ?? DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

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

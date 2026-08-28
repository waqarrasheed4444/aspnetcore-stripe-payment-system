using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Subscriptions.Dtos;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Subscriptions.Commands.CreateSubscriptionCheckout;

public class CreateSubscriptionCheckoutCommand : IRequest<SubscriptionCheckoutResponseDto>
{
    public string UserId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string StripePriceId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class CreateSubscriptionCheckoutCommandValidator : AbstractValidator<CreateSubscriptionCheckoutCommand>
{
    public CreateSubscriptionCheckoutCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().WithMessage("Valid email is required.");
        RuleFor(x => x.CustomerName).NotEmpty().WithMessage("Customer name is required.");
        RuleFor(x => x.StripePriceId).NotEmpty().WithMessage("Stripe Price ID is required (e.g. price_12345).");
        RuleFor(x => x.PlanName).NotEmpty().WithMessage("Plan name is required.");
    }
}

public class CreateSubscriptionCheckoutCommandHandler : IRequestHandler<CreateSubscriptionCheckoutCommand, SubscriptionCheckoutResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IStripeSubscriptionService _stripeSubscriptionService;

    public CreateSubscriptionCheckoutCommandHandler(
        IApplicationDbContext context,
        IStripePaymentService stripePaymentService,
        IStripeSubscriptionService stripeSubscriptionService)
    {
        _context = context;
        _stripePaymentService = stripePaymentService;
        _stripeSubscriptionService = stripeSubscriptionService;
    }

    public async Task<SubscriptionCheckoutResponseDto> Handle(CreateSubscriptionCheckoutCommand request, CancellationToken cancellationToken)
    {
        // 1. Get or Create Stripe Customer
        var stripeCustomerId = await _stripePaymentService.GetOrCreateCustomerAsync(
            request.UserId,
            request.CustomerEmail,
            request.CustomerName,
            cancellationToken);

        // 2. Create pending internal subscription record
        var subscription = new Subscription
        {
            UserId = request.UserId,
            StripeCustomerId = stripeCustomerId,
            StripePriceId = request.StripePriceId,
            PlanName = request.PlanName,
            Status = SubscriptionStatus.Incomplete
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. Create Subscription Checkout Session via Stripe
        var sessionModel = new CreateSubscriptionCheckoutModel
        {
            CustomerId = stripeCustomerId,
            CustomerEmail = request.CustomerEmail,
            PriceId = request.StripePriceId,
            PlanName = request.PlanName,
            UserId = request.UserId,
            InternalSubscriptionId = subscription.Id,
            SuccessUrl = request.SuccessUrl ?? string.Empty,
            CancelUrl = request.CancelUrl ?? string.Empty,
            Metadata = new Dictionary<string, string>
            {
                { "UserId", request.UserId },
                { "SubscriptionId", subscription.Id.ToString() },
                { "PlanName", request.PlanName }
            }
        };

        var sessionResult = await _stripeSubscriptionService.CreateSubscriptionCheckoutSessionAsync(sessionModel, cancellationToken);

        return new SubscriptionCheckoutResponseDto
        {
            SubscriptionId = subscription.Id,
            SessionId = sessionResult.SessionId,
            CheckoutUrl = sessionResult.SessionUrl,
            PlanName = request.PlanName
        };
    }
}

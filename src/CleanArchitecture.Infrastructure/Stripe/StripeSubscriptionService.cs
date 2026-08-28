using System.Net;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Stripe;

public class StripeSubscriptionService : IStripeSubscriptionService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeSubscriptionService> _logger;

    public StripeSubscriptionService(
        IOptions<StripeSettings> settings,
        ILogger<StripeSubscriptionService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (!string.IsNullOrEmpty(_settings.SecretKey))
        {
            global::Stripe.StripeConfiguration.ApiKey = _settings.SecretKey;
        }
    }

    public async Task<CheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(
        CreateSubscriptionCheckoutModel model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var successUrl = string.IsNullOrWhiteSpace(model.SuccessUrl) ? _settings.SuccessUrl : model.SuccessUrl;
            var cancelUrl = string.IsNullOrWhiteSpace(model.CancelUrl) ? _settings.CancelUrl : model.CancelUrl;

            var options = new global::Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<global::Stripe.Checkout.SessionLineItemOptions>
                {
                    new global::Stripe.Checkout.SessionLineItemOptions
                    {
                        Price = model.PriceId,
                        Quantity = 1
                    }
                },
                Mode = "subscription",
                Customer = !string.IsNullOrEmpty(model.CustomerId) ? model.CustomerId : null,
                CustomerEmail = string.IsNullOrEmpty(model.CustomerId) ? model.CustomerEmail : null,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = model.Metadata,
                SubscriptionData = new global::Stripe.Checkout.SessionSubscriptionDataOptions
                {
                    Metadata = model.Metadata
                }
            };

            var service = new global::Stripe.Checkout.SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Created Stripe Subscription Checkout Session {SessionId} for Plan {PlanName}", session.Id, model.PlanName);

            return new CheckoutSessionResult
            {
                SessionId = session.Id,
                SessionUrl = session.Url,
                CustomerId = session.CustomerId
            };
        }
        catch (global::Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating subscription checkout session for Plan {PlanName}: {Message}", model.PlanName, ex.Message);
            throw new PaymentException($"Stripe subscription checkout error: {ex.StripeError?.Message ?? ex.Message}", ex, HttpStatusCode.BadRequest);
        }
    }

    public async Task<string> CreateCustomerPortalSessionAsync(
        string stripeCustomerId,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new global::Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = stripeCustomerId,
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? _settings.PortalReturnUrl : returnUrl
            };

            var service = new global::Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Created Stripe Customer Portal Session for Customer {CustomerId}", stripeCustomerId);
            return session.Url;
        }
        catch (global::Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating customer portal session for Customer {CustomerId}: {Message}", stripeCustomerId, ex.Message);
            throw new PaymentException($"Stripe customer portal error: {ex.StripeError?.Message ?? ex.Message}", ex, HttpStatusCode.BadRequest);
        }
    }

    public async Task<SubscriptionCancellationResult> CancelSubscriptionAsync(
        string stripeSubscriptionId,
        bool cancelImmediately = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new global::Stripe.SubscriptionService();

            if (cancelImmediately)
            {
                var canceledSub = await service.CancelAsync(stripeSubscriptionId, cancellationToken: cancellationToken);
                _logger.LogInformation("Immediately canceled Stripe Subscription {SubId}", stripeSubscriptionId);
                return new SubscriptionCancellationResult
                {
                    SubscriptionId = canceledSub.Id,
                    Status = canceledSub.Status,
                    CancelAtPeriodEnd = false,
                    CanceledAt = canceledSub.CanceledAt
                };
            }
            else
            {
                var updateOptions = new global::Stripe.SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                };
                var updatedSub = await service.UpdateAsync(stripeSubscriptionId, updateOptions, cancellationToken: cancellationToken);
                _logger.LogInformation("Scheduled Stripe Subscription {SubId} for cancellation at period end", stripeSubscriptionId);
                return new SubscriptionCancellationResult
                {
                    SubscriptionId = updatedSub.Id,
                    Status = updatedSub.Status,
                    CancelAtPeriodEnd = true,
                    CanceledAt = updatedSub.CanceledAt
                };
            }
        }
        catch (global::Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe error cancelling subscription {SubId}: {Message}", stripeSubscriptionId, ex.Message);
            throw new PaymentException($"Stripe cancel subscription error: {ex.StripeError?.Message ?? ex.Message}", ex, HttpStatusCode.BadRequest);
        }
    }
}

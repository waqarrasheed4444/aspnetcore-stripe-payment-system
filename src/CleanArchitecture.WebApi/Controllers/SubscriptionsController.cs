using CleanArchitecture.Application.Subscriptions.Commands.CancelSubscription;
using CleanArchitecture.Application.Subscriptions.Commands.CreateCustomerPortalSession;
using CleanArchitecture.Application.Subscriptions.Commands.CreateSubscriptionCheckout;
using CleanArchitecture.Application.Subscriptions.Dtos;
using CleanArchitecture.Application.Subscriptions.Queries.GetSubscriptionByUserId;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebApi.Controllers;

/// <summary>
/// Stripe subscription management: create, cancel, and manage billing portal.
/// Subscription status is NEVER marked active solely from checkout — it is
/// confirmed exclusively via Stripe webhook events.
/// </summary>
public class SubscriptionsController : ApiControllerBase
{
    /// <summary>
    /// Creates a Stripe Checkout Session for a subscription plan.
    /// The internal subscription record starts as Incomplete until
    /// the customer.subscription.created webhook is received.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(SubscriptionCheckoutResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionCheckoutResponseDto>> CreateSubscriptionCheckout(
        [FromBody] CreateSubscriptionCheckoutCommand command)
    {
        return await Mediator.Send(command);
    }

    /// <summary>
    /// Creates a Stripe Customer Billing Portal session URL.
    /// Redirects the customer to self-service their subscription (upgrade, cancel, update payment).
    /// </summary>
    [HttpPost("customer-portal")]
    [ProducesResponseType(typeof(CustomerPortalResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerPortalResponseDto>> CreateCustomerPortalSession(
        [FromBody] CreateCustomerPortalSessionCommand command)
    {
        return await Mediator.Send(command);
    }

    /// <summary>
    /// Cancels an active subscription. By default, cancels at period end.
    /// Set cancelImmediately=true to cancel instantly.
    /// </summary>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionDto>> Cancel([FromBody] CancelSubscriptionCommand command)
    {
        return await Mediator.Send(command);
    }

    /// <summary>
    /// Gets the active subscription for a given user.
    /// Returns null if no subscription exists.
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<SubscriptionDto?>> GetByUserId(string userId)
    {
        var result = await Mediator.Send(new GetSubscriptionByUserIdQuery(userId));
        if (result == null) return NoContent();
        return result;
    }
}

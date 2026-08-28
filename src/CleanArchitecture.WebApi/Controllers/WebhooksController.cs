using CleanArchitecture.Application.Payments.Commands.ProcessStripeWebhook;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebApi.Controllers;

/// <summary>
/// Stripe webhook receiver. This is the AUTHORITATIVE source for all payment
/// and subscription status updates. Every event is verified using the
/// Stripe-Signature header before processing.
///
/// IMPORTANT: Do not mark orders as paid based solely on the user
/// reaching the success page. Only trust this webhook endpoint.
/// </summary>
[Route("api/webhooks")]
[ApiController]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IMediator mediator, ILogger<WebhooksController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Stripe webhook endpoint. Reads raw body for signature verification.
    /// Configure in Stripe Dashboard: POST /api/webhooks/stripe
    ///
    /// Local testing:
    ///   stripe listen --forward-to https://localhost:5001/api/webhooks/stripe
    /// </summary>
    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StripeWebhook()
    {
        // Must read raw body for Stripe signature verification
        string jsonPayload;
        using (var reader = new StreamReader(HttpContext.Request.Body))
        {
            jsonPayload = await reader.ReadToEndAsync();
        }

        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrEmpty(stripeSignature))
        {
            _logger.LogWarning("Stripe webhook received without Stripe-Signature header.");
            return BadRequest(new { error = "Missing Stripe-Signature header." });
        }

        if (string.IsNullOrEmpty(jsonPayload))
        {
            _logger.LogWarning("Stripe webhook received with empty payload.");
            return BadRequest(new { error = "Empty webhook payload." });
        }

        try
        {
            var result = await _mediator.Send(new ProcessStripeWebhookCommand
            {
                JsonPayload = jsonPayload,
                StripeSignature = stripeSignature
            });

            return Ok(new
            {
                received = true,
                eventId = result.EventId,
                eventType = result.EventType,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            // Return 400 to tell Stripe to retry; log the actual error
            _logger.LogError(ex, "Error processing Stripe webhook: {Message}", ex.Message);
            return BadRequest(new { error = "Webhook processing failed." });
        }
    }
}

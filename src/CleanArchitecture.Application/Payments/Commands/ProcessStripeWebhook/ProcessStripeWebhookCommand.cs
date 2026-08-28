using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Payments.Commands.ProcessStripeWebhook;

public class ProcessStripeWebhookCommand : IRequest<WebhookProcessResultDto>
{
    public string JsonPayload { get; set; } = string.Empty;
    public string StripeSignature { get; set; } = string.Empty;
}

public class WebhookProcessResultDto
{
    public bool Success { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ProcessStripeWebhookCommandHandler : IRequestHandler<ProcessStripeWebhookCommand, WebhookProcessResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly ILogger<ProcessStripeWebhookCommandHandler> _logger;

    public ProcessStripeWebhookCommandHandler(
        IApplicationDbContext context,
        IStripePaymentService stripePaymentService,
        ILogger<ProcessStripeWebhookCommandHandler> logger)
    {
        _context = context;
        _stripePaymentService = stripePaymentService;
        _logger = logger;
    }

    public async Task<WebhookProcessResultDto> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate signature & parse event
        var webhookEvent = await _stripePaymentService.ParseAndValidateWebhookAsync(
            request.JsonPayload,
            request.StripeSignature,
            cancellationToken);

        _logger.LogInformation("Processing Stripe Webhook Event: {EventId} of type {EventType}", webhookEvent.EventId, webhookEvent.EventType);

        // 2. Idempotency Check: Check if event was already processed
        var alreadyProcessed = await _context.ProcessedWebhookEvents
            .AnyAsync(e => e.StripeEventId == webhookEvent.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Stripe Webhook Event {EventId} has already been processed. Skipping to maintain idempotency.", webhookEvent.EventId);
            return new WebhookProcessResultDto
            {
                Success = true,
                EventId = webhookEvent.EventId,
                EventType = webhookEvent.EventType,
                Message = "Event already processed (idempotent duplicate)."
            };
        }

        // 3. Process event based on type
        await DispatchEventAsync(webhookEvent, cancellationToken);

        // 4. Mark event as processed
        _context.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
        {
            StripeEventId = webhookEvent.EventId,
            EventType = webhookEvent.EventType,
            ProcessedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new WebhookProcessResultDto
        {
            Success = true,
            EventId = webhookEvent.EventId,
            EventType = webhookEvent.EventType,
            Message = "Event processed successfully."
        };
    }

    private async Task DispatchEventAsync(StripeWebhookEventResult webhookEvent, CancellationToken cancellationToken)
    {
        switch (webhookEvent.EventType)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompletedAsync(webhookEvent.DataObject, cancellationToken);
                break;

            case "payment_intent.succeeded":
                await HandlePaymentIntentSucceededAsync(webhookEvent.DataObject, cancellationToken);
                break;

            case "payment_intent.payment_failed":
                await HandlePaymentIntentFailedAsync(webhookEvent.DataObject, cancellationToken);
                break;

            case "charge.refunded":
                await HandleChargeRefundedAsync(webhookEvent.DataObject, cancellationToken);
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await HandleSubscriptionChangedAsync(webhookEvent.DataObject, webhookEvent.EventType, cancellationToken);
                break;

            case "invoice.paid":
                await HandleInvoicePaidAsync(webhookEvent.DataObject, cancellationToken);
                break;

            case "invoice.payment_failed":
                await HandleInvoicePaymentFailedAsync(webhookEvent.DataObject, cancellationToken);
                break;

            default:
                _logger.LogInformation("Unhandled Stripe Webhook Event type: {EventType}", webhookEvent.EventType);
                break;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(object? dataObject, CancellationToken cancellationToken)
    {
        if (dataObject is not Stripe.Checkout.Session session)
            return;

        _logger.LogInformation("Handling checkout.session.completed for Session ID: {SessionId}", session.Id);

        // Check if mode is subscription
        if (session.Mode == "subscription")
        {
            await HandleSubscriptionCheckoutCompletedAsync(session, cancellationToken);
            return;
        }

        // Try lookup by PaymentId metadata
        Payment? payment = null;
        if (session.Metadata != null && session.Metadata.TryGetValue("PaymentId", out var paymentIdStr) && int.TryParse(paymentIdStr, out var paymentId))
        {
            payment = await _context.Payments.Include(p => p.Order).FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
        }

        if (payment == null)
        {
            payment = await _context.Payments.Include(p => p.Order).FirstOrDefaultAsync(p => p.StripeCheckoutSessionId == session.Id, cancellationToken);
        }

        if (payment != null)
        {
            payment.Status = PaymentStatus.Paid;
            payment.StripePaymentIntentId = session.PaymentIntentId ?? payment.StripePaymentIntentId;

            if (payment.Order != null)
            {
                payment.Order.Status = OrderStatus.Paid;
            }

            var transaction = new PaymentTransaction
            {
                PaymentId = payment.Id,
                StripeChargeId = session.PaymentIntentId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Type = PaymentTransactionType.Payment,
                Status = "Succeeded"
            };
            _context.PaymentTransactions.Add(transaction);
        }
    }

    private async Task HandleSubscriptionCheckoutCompletedAsync(Stripe.Checkout.Session session, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(session.SubscriptionId))
            return;

        Subscription? subscription = null;
        if (session.Metadata != null && session.Metadata.TryGetValue("SubscriptionId", out var subIdStr) && int.TryParse(subIdStr, out var subId))
        {
            subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == subId, cancellationToken);
        }

        if (subscription != null)
        {
            subscription.StripeSubscriptionId = session.SubscriptionId;
            subscription.StripeCustomerId = session.CustomerId ?? subscription.StripeCustomerId;
            subscription.Status = SubscriptionStatus.Active;
        }
    }

    private async Task HandlePaymentIntentSucceededAsync(object? dataObject, CancellationToken cancellationToken)
    {
        if (dataObject is not Stripe.PaymentIntent intent)
            return;

        _logger.LogInformation("Handling payment_intent.succeeded for Intent ID: {IntentId}", intent.Id);

        var payment = await _context.Payments.Include(p => p.Order).FirstOrDefaultAsync(p => p.StripePaymentIntentId == intent.Id, cancellationToken);
        if (payment != null && payment.Status != PaymentStatus.Paid)
        {
            payment.Status = PaymentStatus.Paid;
            if (payment.Order != null)
            {
                payment.Order.Status = OrderStatus.Paid;
            }
        }
    }

    private async Task HandlePaymentIntentFailedAsync(object? dataObject, CancellationToken cancellationToken)
    {
        if (dataObject is not Stripe.PaymentIntent intent)
            return;

        _logger.LogWarning("Handling payment_intent.payment_failed for Intent ID: {IntentId}", intent.Id);

        var payment = await _context.Payments.Include(p => p.Order).FirstOrDefaultAsync(p => p.StripePaymentIntentId == intent.Id, cancellationToken);
        if (payment != null)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureMessage = intent.LastPaymentError?.Message ?? "Payment failed.";

            if (payment.Order != null)
            {
                payment.Order.Status = OrderStatus.Failed;
            }

            var transaction = new PaymentTransaction
            {
                PaymentId = payment.Id,
                StripeChargeId = intent.Id,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Type = PaymentTransactionType.Payment,
                Status = "Failed",
                FailureReason = payment.FailureMessage
            };
            _context.PaymentTransactions.Add(transaction);
        }
    }

    private async Task HandleChargeRefundedAsync(object? dataObject, CancellationToken cancellationToken)
    {
        if (dataObject is not Stripe.Charge charge)
            return;

        _logger.LogInformation("Handling charge.refunded for Charge ID: {ChargeId}", charge.Id);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.StripePaymentIntentId == charge.PaymentIntentId, cancellationToken);
        if (payment != null)
        {
            decimal totalRefunded = (decimal)charge.AmountRefunded / 100m;
            payment.AmountRefunded = totalRefunded;
            payment.Status = charge.Refunded ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        }
    }

    private async Task HandleSubscriptionChangedAsync(object? dataObject, string eventType, CancellationToken cancellationToken)
    {
        if (dataObject is not Stripe.Subscription stripeSub)
            return;

        _logger.LogInformation("Handling {EventType} for Subscription ID: {SubId}", eventType, stripeSub.Id);

        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id, cancellationToken);
        if (subscription == null)
            return;

        subscription.Status = stripeSub.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trialing,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "unpaid" => SubscriptionStatus.Unpaid,
            "incomplete" => SubscriptionStatus.Incomplete,
            "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
            _ => subscription.Status
        };

        subscription.CurrentPeriodStart = stripeSub.CurrentPeriodStart;
        subscription.CurrentPeriodEnd = stripeSub.CurrentPeriodEnd;
        subscription.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;
        subscription.CanceledAt = stripeSub.CanceledAt;
    }

    private async Task HandleInvoicePaidAsync(object? dataObject, CancellationToken cancellationToken)
    {
        if (dataObject is not Stripe.Invoice invoice)
            return;

        _logger.LogInformation("Handling invoice.paid for Subscription ID: {SubId}", invoice.SubscriptionId);

        if (!string.IsNullOrEmpty(invoice.SubscriptionId))
        {
            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == invoice.SubscriptionId, cancellationToken);
            if (subscription != null)
            {
                subscription.Status = SubscriptionStatus.Active;
            }
        }
    }

    private async Task HandleInvoicePaymentFailedAsync(object? dataObject, CancellationToken cancellationToken)
    {
        if (dataObject is not Stripe.Invoice invoice)
            return;

        _logger.LogWarning("Handling invoice.payment_failed for Subscription ID: {SubId}", invoice.SubscriptionId);

        if (!string.IsNullOrEmpty(invoice.SubscriptionId))
        {
            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == invoice.SubscriptionId, cancellationToken);
            if (subscription != null)
            {
                subscription.Status = SubscriptionStatus.PastDue;
            }
        }
    }
}

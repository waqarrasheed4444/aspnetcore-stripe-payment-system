using System.Net;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Stripe;

public class StripePaymentService : IStripePaymentService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly IApplicationDbContext _context;

    public StripePaymentService(
        IOptions<StripeSettings> settings,
        ILogger<StripePaymentService> logger,
        IApplicationDbContext context)
    {
        _settings = settings.Value;
        _logger = logger;
        _context = context;

        if (!string.IsNullOrEmpty(_settings.SecretKey))
        {
            global::Stripe.StripeConfiguration.ApiKey = _settings.SecretKey;
        }
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionModel model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lineItems = new List<global::Stripe.Checkout.SessionLineItemOptions>();

            if (model.Items.Any())
            {
                foreach (var item in model.Items)
                {
                    lineItems.Add(new global::Stripe.Checkout.SessionLineItemOptions
                    {
                        PriceData = new global::Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.UnitAmount * 100),
                            Currency = model.Currency.ToLowerInvariant(),
                            ProductData = new global::Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Name,
                                Description = item.Description
                            }
                        },
                        Quantity = item.Quantity
                    });
                }
            }
            else
            {
                lineItems.Add(new global::Stripe.Checkout.SessionLineItemOptions
                {
                    PriceData = new global::Stripe.Checkout.SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(model.Amount * 100),
                        Currency = model.Currency.ToLowerInvariant(),
                        ProductData = new global::Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                        {
                            Name = model.Description
                        }
                    },
                    Quantity = 1
                });
            }

            var successUrl = string.IsNullOrWhiteSpace(model.SuccessUrl) ? _settings.SuccessUrl : model.SuccessUrl;
            var cancelUrl = string.IsNullOrWhiteSpace(model.CancelUrl) ? _settings.CancelUrl : model.CancelUrl;

            var options = new global::Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                Customer = !string.IsNullOrEmpty(model.CustomerId) ? model.CustomerId : null,
                CustomerEmail = string.IsNullOrEmpty(model.CustomerId) ? model.CustomerEmail : null,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = model.Metadata,
                PaymentIntentData = new global::Stripe.Checkout.SessionPaymentIntentDataOptions
                {
                    Metadata = model.Metadata,
                    Description = model.Description
                }
            };

            var service = new global::Stripe.Checkout.SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Created Stripe Checkout Session {SessionId} for Order {OrderId}", session.Id, model.OrderId);

            return new CheckoutSessionResult
            {
                SessionId = session.Id,
                SessionUrl = session.Url,
                PaymentIntentId = session.PaymentIntentId,
                CustomerId = session.CustomerId
            };
        }
        catch (global::Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session for Order {OrderId}: {Message}", model.OrderId, ex.Message);
            throw new PaymentException($"Stripe error: {ex.StripeError?.Message ?? ex.Message}", ex, HttpStatusCode.BadRequest);
        }
    }

    public async Task<RefundResult> CreateRefundAsync(
        string paymentIntentId,
        decimal amount,
        string currency,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new global::Stripe.RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = (long)(amount * 100),
                Reason = reason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    _ => "requested_by_customer"
                }
            };

            var service = new global::Stripe.RefundService();
            var refund = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully issued Stripe refund {RefundId} for PaymentIntent {PaymentIntentId}", refund.Id, paymentIntentId);

            return new RefundResult
            {
                RefundId = refund.Id,
                PaymentIntentId = paymentIntentId,
                ChargeId = refund.ChargeId,
                AmountRefunded = (decimal)refund.Amount / 100m,
                Currency = refund.Currency,
                Status = refund.Status,
                Reason = refund.Reason
            };
        }
        catch (global::Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating refund for PaymentIntent {PaymentIntentId}: {Message}", paymentIntentId, ex.Message);
            throw new PaymentException($"Stripe refund error: {ex.StripeError?.Message ?? ex.Message}", ex, HttpStatusCode.BadRequest);
        }
    }

    public async Task<string> GetOrCreateCustomerAsync(
        string userId,
        string email,
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check local DB first
            var existingRecord = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(_context.StripeCustomers, c => c.UserId == userId, cancellationToken);
            
            var existing = await existingRecord;
            if (existing != null && !string.IsNullOrEmpty(existing.StripeCustomerId))
            {
                return existing.StripeCustomerId;
            }

            // Search in Stripe to prevent duplicates
            var customerService = new global::Stripe.CustomerService();
            var searchOptions = new global::Stripe.CustomerSearchOptions
            {
                Query = $"email:\'{email}\'"
            };

            var searchResult = await customerService.SearchAsync(searchOptions, cancellationToken: cancellationToken);
            var customer = searchResult.Data.FirstOrDefault();

            if (customer == null)
            {
                var createOptions = new global::Stripe.CustomerCreateOptions
                {
                    Email = email,
                    Name = name,
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserId", userId }
                    }
                };

                customer = await customerService.CreateAsync(createOptions, cancellationToken: cancellationToken);
                _logger.LogInformation("Created new Stripe Customer {CustomerId} for User {UserId}", customer.Id, userId);
            }
            else
            {
                _logger.LogInformation("Found existing Stripe Customer {CustomerId} for email {Email}", customer.Id, email);
            }

            // Persist mapping
            if (existing == null)
            {
                _context.StripeCustomers.Add(new Domain.Entities.StripeCustomer
                {
                    UserId = userId,
                    Email = email,
                    Name = name,
                    StripeCustomerId = customer.Id
                });
            }
            else
            {
                existing.StripeCustomerId = customer.Id;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return customer.Id;
        }
        catch (global::Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe error getting/creating customer for User {UserId}: {Message}", userId, ex.Message);
            throw new PaymentException($"Stripe customer error: {ex.StripeError?.Message ?? ex.Message}", ex, HttpStatusCode.BadRequest);
        }
    }

    public Task<StripeWebhookEventResult> ParseAndValidateWebhookAsync(
        string jsonPayload,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
            {
                throw new PaymentException("Stripe WebhookSecret is not configured on the server.", HttpStatusCode.InternalServerError);
            }

            var stripeEvent = global::Stripe.EventUtility.ConstructEvent(
                jsonPayload,
                stripeSignatureHeader,
                _settings.WebhookSecret,
                throwOnApiVersionMismatch: false);

            return Task.FromResult(new StripeWebhookEventResult
            {
                EventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                DataObject = stripeEvent.Data.Object,
                RawJson = jsonPayload
            });
        }
        catch (global::Stripe.StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature validation failed: {Message}", ex.Message);
            throw new PaymentException($"Invalid Stripe webhook signature: {ex.Message}", ex, HttpStatusCode.BadRequest);
        }
        catch (Exception ex) when (ex is not PaymentException)
        {
            _logger.LogError(ex, "Unexpected error parsing Stripe webhook: {Message}", ex.Message);
            throw new PaymentException($"Webhook parsing error: {ex.Message}", ex, HttpStatusCode.BadRequest);
        }
    }
}

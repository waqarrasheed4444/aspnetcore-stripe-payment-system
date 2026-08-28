using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Payments.Dtos;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Payments.Commands.CreatePaymentCheckout;

public class CreatePaymentCheckoutCommand : IRequest<CheckoutSessionResponseDto>
{
    public int OrderId { get; set; }
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class CreatePaymentCheckoutCommandValidator : AbstractValidator<CreatePaymentCheckoutCommand>
{
    public CreatePaymentCheckoutCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("Valid OrderId is required.");
    }
}

public class CreatePaymentCheckoutCommandHandler : IRequestHandler<CreatePaymentCheckoutCommand, CheckoutSessionResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IStripePaymentService _stripePaymentService;

    public CreatePaymentCheckoutCommandHandler(
        IApplicationDbContext context,
        IStripePaymentService stripePaymentService)
    {
        _context = context;
        _stripePaymentService = stripePaymentService;
    }

    public async Task<CheckoutSessionResponseDto> Handle(CreatePaymentCheckoutCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException($"Order with ID {request.OrderId} was not found.");
        }

        if (order.Status == OrderStatus.Paid)
        {
            throw new PaymentException("Order has already been paid.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new PaymentException("Cannot checkout a cancelled order.");
        }

        if (!order.Items.Any())
        {
            throw new PaymentException("Order contains no items.");
        }

        // 1. Get or Create Stripe Customer
        var stripeCustomerId = await _stripePaymentService.GetOrCreateCustomerAsync(
            order.UserId,
            order.CustomerEmail,
            order.CustomerName,
            cancellationToken);

        // 2. Create or reuse internal Payment record
        Payment payment;
        if (order.Payment != null && order.Payment.Status == PaymentStatus.Pending)
        {
            payment = order.Payment;
            payment.Amount = order.TotalAmount;
            payment.Currency = order.Currency;
            payment.StripeCustomerId = stripeCustomerId;
        }
        else
        {
            payment = new Payment
            {
                OrderId = order.Id,
                UserId = order.UserId,
                StripeCustomerId = stripeCustomerId,
                Amount = order.TotalAmount,
                Currency = order.Currency,
                Status = PaymentStatus.Pending,
                Description = $"Payment for Order #{order.Id}"
            };
            _context.Payments.Add(payment);
            order.Payment = payment;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 3. Create Stripe Checkout Session
        var checkoutModel = new CreateCheckoutSessionModel
        {
            CustomerId = stripeCustomerId,
            CustomerEmail = order.CustomerEmail,
            OrderId = order.Id.ToString(),
            UserId = order.UserId,
            InternalPaymentId = payment.Id,
            Amount = order.TotalAmount,
            Currency = order.Currency,
            Description = $"Payment for Order #{order.Id}",
            SuccessUrl = request.SuccessUrl ?? string.Empty,
            CancelUrl = request.CancelUrl ?? string.Empty,
            Items = order.Items.Select(i => new CheckoutItemModel
            {
                Name = i.ProductName,
                UnitAmount = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList(),
            Metadata = new Dictionary<string, string>
            {
                { "OrderId", order.Id.ToString() },
                { "UserId", order.UserId },
                { "PaymentId", payment.Id.ToString() }
            }
        };

        var sessionResult = await _stripePaymentService.CreateCheckoutSessionAsync(checkoutModel, cancellationToken);

        // 4. Update Payment with Session ID
        payment.StripeCheckoutSessionId = sessionResult.SessionId;
        await _context.SaveChangesAsync(cancellationToken);

        return new CheckoutSessionResponseDto
        {
            PaymentId = payment.Id,
            SessionId = sessionResult.SessionId,
            CheckoutUrl = sessionResult.SessionUrl,
            Amount = payment.Amount,
            Currency = payment.Currency
        };
    }
}

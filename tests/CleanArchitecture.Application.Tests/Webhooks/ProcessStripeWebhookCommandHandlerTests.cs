using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Payments.Commands.ProcessStripeWebhook;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CleanArchitecture.Application.Tests.Webhooks;

public class ProcessStripeWebhookCommandHandlerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStripePaymentService> _stripePaymentServiceMock;
    private readonly ProcessStripeWebhookCommandHandler _handler;

    public ProcessStripeWebhookCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _stripePaymentServiceMock = new Mock<IStripePaymentService>();
        _handler = new ProcessStripeWebhookCommandHandler(
            _context,
            _stripePaymentServiceMock.Object,
            NullLogger<ProcessStripeWebhookCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ShouldProcessCheckoutSessionCompleted_AndMarkPaymentAndOrderPaid()
    {
        // Arrange
        var order = new Order
        {
            UserId = "user-1",
            CustomerEmail = "test@example.com",
            CustomerName = "Test User",
            TotalAmount = 49.99m,
            Currency = "usd",
            Status = OrderStatus.Pending
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var payment = new Payment
        {
            OrderId = order.Id,
            UserId = "user-1",
            StripeCheckoutSessionId = "cs_webhook_123",
            Amount = 49.99m,
            Currency = "usd",
            Status = PaymentStatus.Pending
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var session = new Stripe.Checkout.Session
        {
            Id = "cs_webhook_123",
            PaymentIntentId = "pi_session_paid_999",
            Metadata = new Dictionary<string, string>
            {
                { "PaymentId", payment.Id.ToString() },
                { "OrderId", order.Id.ToString() }
            }
        };

        _stripePaymentServiceMock
            .Setup(s => s.ParseAndValidateWebhookAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeWebhookEventResult
            {
                EventId = "evt_session_completed_001",
                EventType = "checkout.session.completed",
                DataObject = session
            });

        var command = new ProcessStripeWebhookCommand
        {
            JsonPayload = "{\"id\":\"evt_session_completed_001\"}",
            StripeSignature = "sig_valid"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.EventId.Should().Be("evt_session_completed_001");

        var updatedPayment = await _context.Payments.Include(p => p.Transactions).FirstOrDefaultAsync(p => p.Id == payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Paid);
        updatedPayment.StripePaymentIntentId.Should().Be("pi_session_paid_999");
        updatedPayment.Transactions.Should().HaveCount(1);
        updatedPayment.Transactions.First().Type.Should().Be(PaymentTransactionType.Payment);
        updatedPayment.Transactions.First().Status.Should().Be("Succeeded");

        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Paid);

        // Verify event recorded in ProcessedWebhookEvents
        var processed = await _context.ProcessedWebhookEvents.AnyAsync(e => e.StripeEventId == "evt_session_completed_001");
        processed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldIgnoreDuplicateWebhook_AndMaintainIdempotency()
    {
        // Arrange: Pre-populate event in ProcessedWebhookEvents
        _context.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
        {
            StripeEventId = "evt_duplicate_999",
            EventType = "checkout.session.completed",
            ProcessedAt = DateTime.UtcNow.AddMinutes(-5)
        });
        await _context.SaveChangesAsync();

        _stripePaymentServiceMock
            .Setup(s => s.ParseAndValidateWebhookAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeWebhookEventResult
            {
                EventId = "evt_duplicate_999",
                EventType = "checkout.session.completed",
                DataObject = new Stripe.Checkout.Session { Id = "cs_dup" }
            });

        var command = new ProcessStripeWebhookCommand
        {
            JsonPayload = "{\"id\":\"evt_duplicate_999\"}",
            StripeSignature = "sig_valid"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("already processed");
    }

    [Fact]
    public async Task Handle_ShouldHandlePaymentIntentFailed_AndMarkFailed()
    {
        // Arrange
        var order = new Order
        {
            UserId = "user-1",
            CustomerEmail = "test@example.com",
            CustomerName = "Test User",
            TotalAmount = 25m,
            Currency = "usd",
            Status = OrderStatus.Pending
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var payment = new Payment
        {
            OrderId = order.Id,
            UserId = "user-1",
            StripePaymentIntentId = "pi_failed_123",
            Amount = 25m,
            Currency = "usd",
            Status = PaymentStatus.Pending
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var intent = new Stripe.PaymentIntent
        {
            Id = "pi_failed_123",
            LastPaymentError = new Stripe.StripeError { Message = "Your card was declined." }
        };

        _stripePaymentServiceMock
            .Setup(s => s.ParseAndValidateWebhookAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeWebhookEventResult
            {
                EventId = "evt_failed_001",
                EventType = "payment_intent.payment_failed",
                DataObject = intent
            });

        var command = new ProcessStripeWebhookCommand
        {
            JsonPayload = "{\"id\":\"evt_failed_001\"}",
            StripeSignature = "sig_valid"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var updatedPayment = await _context.Payments.Include(p => p.Transactions).FirstOrDefaultAsync(p => p.Id == payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Failed);
        updatedPayment.FailureMessage.Should().Be("Your card was declined.");
        updatedPayment.Transactions.Should().HaveCount(1);
        updatedPayment.Transactions.First().Status.Should().Be("Failed");

        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Failed);
    }
}

using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Payments.Commands.RefundPayment;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CleanArchitecture.Application.Tests.Payments;

public class RefundPaymentCommandHandlerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStripePaymentService> _stripePaymentServiceMock;
    private readonly RefundPaymentCommandHandler _handler;

    public RefundPaymentCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _stripePaymentServiceMock = new Mock<IStripePaymentService>();
        _handler = new RefundPaymentCommandHandler(_context, _stripePaymentServiceMock.Object);
    }

    private async Task<Payment> CreatePaidPaymentAsync(decimal amount = 100m)
    {
        var payment = new Payment
        {
            UserId = "user-1",
            StripePaymentIntentId = "pi_test_123",
            StripeCustomerId = "cus_test",
            Amount = amount,
            AmountRefunded = 0,
            Currency = "usd",
            Status = PaymentStatus.Paid
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    [Fact]
    public async Task Handle_ShouldIssueFullRefund_WhenAmountNotSpecified()
    {
        // Arrange
        var payment = await CreatePaidPaymentAsync(100m);

        _stripePaymentServiceMock
            .Setup(s => s.CreateRefundAsync("pi_test_123", 100m, "usd", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundResult { RefundId = "re_test_full", ChargeId = "ch_test", AmountRefunded = 100m, Currency = "usd", Status = "succeeded" });

        // Act
        var result = await _handler.Handle(new RefundPaymentCommand { PaymentId = payment.Id }, CancellationToken.None);

        // Assert
        result.AmountRefunded.Should().Be(100m);
        result.TotalAmountRefunded.Should().Be(100m);
        result.PaymentStatus.Should().Be(PaymentStatus.Refunded);

        var updated = await _context.Payments.FindAsync(payment.Id);
        updated!.Status.Should().Be(PaymentStatus.Refunded);
        updated.AmountRefunded.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_ShouldIssuePartialRefund_AndMarkPartiallyRefunded()
    {
        // Arrange
        var payment = await CreatePaidPaymentAsync(100m);

        _stripePaymentServiceMock
            .Setup(s => s.CreateRefundAsync("pi_test_123", 30m, "usd", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundResult { RefundId = "re_partial", ChargeId = "ch_test", AmountRefunded = 30m, Currency = "usd", Status = "succeeded" });

        // Act
        var result = await _handler.Handle(new RefundPaymentCommand { PaymentId = payment.Id, Amount = 30m }, CancellationToken.None);

        // Assert
        result.AmountRefunded.Should().Be(30m);
        result.PaymentStatus.Should().Be(PaymentStatus.PartiallyRefunded);

        var updated = await _context.Payments.FindAsync(payment.Id);
        updated!.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        updated.AmountRefunded.Should().Be(30m);
    }

    [Fact]
    public async Task Handle_ShouldThrowPaymentException_WhenRefundExceedsAmount()
    {
        // Arrange
        var payment = await CreatePaidPaymentAsync(50m);

        // Act
        Func<Task> act = async () => await _handler.Handle(
            new RefundPaymentCommand { PaymentId = payment.Id, Amount = 999m }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PaymentException>().WithMessage("*exceeds maximum refundable*");
    }

    [Fact]
    public async Task Handle_ShouldThrowPaymentException_WhenPaymentNotPaid()
    {
        // Arrange
        var payment = new Payment
        {
            UserId = "user-1",
            StripePaymentIntentId = "pi_failed",
            StripeCustomerId = "cus_test",
            Amount = 50m,
            Currency = "usd",
            Status = PaymentStatus.Pending  // Not paid
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _handler.Handle(
            new RefundPaymentCommand { PaymentId = payment.Id }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PaymentException>().WithMessage("*Only paid*");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenPaymentDoesNotExist()
    {
        // Act
        Func<Task> act = async () => await _handler.Handle(
            new RefundPaymentCommand { PaymentId = 9999 }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

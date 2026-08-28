using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Subscriptions.Commands.CancelSubscription;
using CleanArchitecture.Application.Subscriptions.Commands.CreateSubscriptionCheckout;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CleanArchitecture.Application.Tests.Subscriptions;

public class SubscriptionCommandHandlerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStripePaymentService> _stripePaymentServiceMock;
    private readonly Mock<IStripeSubscriptionService> _stripeSubscriptionServiceMock;

    public SubscriptionCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _stripePaymentServiceMock = new Mock<IStripePaymentService>();
        _stripeSubscriptionServiceMock = new Mock<IStripeSubscriptionService>();
    }

    [Fact]
    public async Task CreateSubscriptionCheckout_ShouldCreatePendingSubscriptionAndReturnCheckoutUrl()
    {
        // Arrange
        _stripePaymentServiceMock
            .Setup(s => s.GetOrCreateCustomerAsync("user-sub-1", "sub@example.com", "Sub User", It.IsAny<CancellationToken>()))
            .ReturnsAsync("cus_sub_123");

        _stripeSubscriptionServiceMock
            .Setup(s => s.CreateSubscriptionCheckoutSessionAsync(It.IsAny<CreateSubscriptionCheckoutModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionResult
            {
                SessionId = "cs_sub_session_789",
                SessionUrl = "https://checkout.stripe.com/c/pay/cs_sub_session_789",
                CustomerId = "cus_sub_123"
            });

        var handler = new CreateSubscriptionCheckoutCommandHandler(
            _context,
            _stripePaymentServiceMock.Object,
            _stripeSubscriptionServiceMock.Object);

        var command = new CreateSubscriptionCheckoutCommand
        {
            UserId = "user-sub-1",
            CustomerEmail = "sub@example.com",
            CustomerName = "Sub User",
            StripePriceId = "price_pro_monthly",
            PlanName = "Pro Monthly"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be("cs_sub_session_789");
        result.CheckoutUrl.Should().Contain("stripe.com");
        result.PlanName.Should().Be("Pro Monthly");

        var createdSub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == "user-sub-1");
        createdSub.Should().NotBeNull();
        createdSub!.Status.Should().Be(SubscriptionStatus.Incomplete); // Awaits webhook confirmation
        createdSub.StripeCustomerId.Should().Be("cus_sub_123");
    }

    [Fact]
    public async Task CancelSubscription_ShouldUpdateSubscriptionStatusAndReturnResult()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = "user-sub-2",
            StripeCustomerId = "cus_sub_456",
            StripeSubscriptionId = "sub_live_stripe_999",
            StripePriceId = "price_enterprise_yearly",
            PlanName = "Enterprise Yearly",
            Status = SubscriptionStatus.Active
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _stripeSubscriptionServiceMock
            .Setup(s => s.CancelSubscriptionAsync("sub_live_stripe_999", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionCancellationResult
            {
                SubscriptionId = "sub_live_stripe_999",
                Status = "active",
                CancelAtPeriodEnd = true,
                CanceledAt = DateTime.UtcNow
            });

        var handler = new CancelSubscriptionCommandHandler(_context, _stripeSubscriptionServiceMock.Object);

        var command = new CancelSubscriptionCommand
        {
            SubscriptionId = subscription.Id,
            CancelImmediately = false
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CancelAtPeriodEnd.Should().BeTrue();

        var updatedSub = await _context.Subscriptions.FindAsync(subscription.Id);
        updatedSub!.CancelAtPeriodEnd.Should().BeTrue();
    }
}

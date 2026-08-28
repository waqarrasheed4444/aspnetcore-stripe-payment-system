using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Payments.Commands.CreatePaymentCheckout;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CleanArchitecture.Application.Tests.Payments;

public class CreatePaymentCheckoutCommandHandlerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStripePaymentService> _stripePaymentServiceMock;
    private readonly CreatePaymentCheckoutCommandHandler _handler;

    public CreatePaymentCheckoutCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _stripePaymentServiceMock = new Mock<IStripePaymentService>();
        _handler = new CreatePaymentCheckoutCommandHandler(_context, _stripePaymentServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateCheckoutSession_WhenOrderIsPending()
    {
        // Arrange
        var category = new Category { Name = "Test", Description = "Test" };
        var product = new Product { Name = "Widget", SKU = "WGT-001", Price = 19.99m, StockQuantity = 5, Status = ProductStatus.Active, Category = category };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = "user-1",
            CustomerEmail = "test@example.com",
            CustomerName = "Test User",
            Currency = "usd",
            TotalAmount = 19.99m,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem> { new() { ProductId = product.Id, ProductName = "Widget", UnitPrice = 19.99m, Quantity = 1 } }
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _stripePaymentServiceMock
            .Setup(s => s.GetOrCreateCustomerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cus_test123");

        _stripePaymentServiceMock
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<CreateCheckoutSessionModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionResult
            {
                SessionId = "cs_test_abc123",
                SessionUrl = "https://checkout.stripe.com/pay/cs_test_abc123",
                CustomerId = "cus_test123"
            });

        var command = new CreatePaymentCheckoutCommand { OrderId = order.Id };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be("cs_test_abc123");
        result.CheckoutUrl.Should().Contain("stripe.com");
        result.Amount.Should().Be(19.99m);
        result.Currency.Should().Be("usd");

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Pending);
        payment.StripeCheckoutSessionId.Should().Be("cs_test_abc123");
        payment.StripeCustomerId.Should().Be("cus_test123");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenOrderDoesNotExist()
    {
        // Act
        Func<Task> act = async () => await _handler.Handle(new CreatePaymentCheckoutCommand { OrderId = 9999 }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowPaymentException_WhenOrderIsAlreadyPaid()
    {
        // Arrange
        var order = new Order
        {
            UserId = "user-2",
            CustomerEmail = "paid@example.com",
            CustomerName = "Paid User",
            Currency = "usd",
            TotalAmount = 10m,
            Status = OrderStatus.Paid
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _handler.Handle(new CreatePaymentCheckoutCommand { OrderId = order.Id }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PaymentException>().WithMessage("*already been paid*");
    }

    [Fact]
    public async Task Handle_ShouldUseExistingStripeCustomer_WhenCustomerAlreadyExists()
    {
        // Arrange
        var stripeCustomer = new StripeCustomer
        {
            UserId = "user-3",
            Email = "returning@example.com",
            Name = "Returning User",
            StripeCustomerId = "cus_existing_456"
        };
        _context.StripeCustomers.Add(stripeCustomer);

        var category = new Category { Name = "Electronics", Description = "Test" };
        var product = new Product { Name = "Headphones", SKU = "HP-001", Price = 50m, StockQuantity = 3, Status = ProductStatus.Active, Category = category };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = "user-3",
            CustomerEmail = "returning@example.com",
            CustomerName = "Returning User",
            Currency = "usd",
            TotalAmount = 50m,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem> { new() { ProductId = product.Id, ProductName = "Headphones", UnitPrice = 50m, Quantity = 1 } }
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _stripePaymentServiceMock
            .Setup(s => s.GetOrCreateCustomerAsync("user-3", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cus_existing_456");

        _stripePaymentServiceMock
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<CreateCheckoutSessionModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionResult { SessionId = "cs_returning", SessionUrl = "https://stripe.com/returning", CustomerId = "cus_existing_456" });

        // Act
        var result = await _handler.Handle(new CreatePaymentCheckoutCommand { OrderId = order.Id }, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _stripePaymentServiceMock.Verify(
            s => s.GetOrCreateCustomerAsync("user-3", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

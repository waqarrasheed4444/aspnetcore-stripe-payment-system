using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Orders.Commands.CreateOrder;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Tests.Orders;

public class CreateOrderCommandHandlerTests
{
    private readonly ApplicationDbContext _context;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _handler = new CreateOrderCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrder_WhenProductsExistAndStockIsSufficient()
    {
        // Arrange
        var category = new Category { Name = "Test", Description = "Test Category" };
        var product = new Product { Name = "Widget", SKU = "WGT-001", Price = 9.99m, StockQuantity = 10, Status = ProductStatus.Active, Category = category };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var command = new CreateOrderCommand
        {
            UserId = "user-1",
            CustomerEmail = "test@example.com",
            CustomerName = "Test User",
            Currency = "usd",
            Items = new List<CreateOrderItemDto> { new() { ProductId = product.Id, Quantity = 2 } }
        };

        // Act
        var orderId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        orderId.Should().BeGreaterThan(0);
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        order.Should().NotBeNull();
        order!.TotalAmount.Should().Be(9.99m * 2);
        order.Currency.Should().Be("usd");
        order.Items.Should().HaveCount(1);
        order.Items.First().UnitPrice.Should().Be(9.99m);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            UserId = "user-1",
            CustomerEmail = "test@example.com",
            CustomerName = "Test User",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 9999, Quantity = 1 } }
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowPaymentException_WhenInsufficientStock()
    {
        // Arrange
        var category = new Category { Name = "Test", Description = "Test Category" };
        var product = new Product { Name = "LowStock", SKU = "LS-001", Price = 5m, StockQuantity = 1, Status = ProductStatus.Active, Category = category };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var command = new CreateOrderCommand
        {
            UserId = "user-1",
            CustomerEmail = "test@example.com",
            CustomerName = "Test User",
            Items = new List<CreateOrderItemDto> { new() { ProductId = product.Id, Quantity = 99 } }
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PaymentException>();
    }
}

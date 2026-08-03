using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Products.Commands.CreateProduct;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Tests.Products;

public class CreateProductCommandHandlerTests
{
    private readonly ApplicationDbContext _context;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _handler = new CreateProductCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_ShouldCreateProduct_WhenRequestIsValid()
    {
        // Arrange
        var category = new Category { Name = "Electronics", Description = "Test" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var command = new CreateProductCommand
        {
            Name = "4K Gaming Monitor",
            SKU = "ELEC-MON-4K",
            Description = "32 inch 144Hz IPS Gaming Monitor",
            Price = 499.99m,
            StockQuantity = 10,
            Status = ProductStatus.Active,
            CategoryId = category.Id
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeGreaterThan(0);

        var createdProduct = await _context.Products.FindAsync(result);
        createdProduct.Should().NotBeNull();
        createdProduct!.Name.Should().Be("4K Gaming Monitor");
        createdProduct.SKU.Should().Be("ELEC-MON-4K");
        createdProduct.Price.Should().Be(499.99m);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenCategoryDoesNotExist()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Invalid Product",
            SKU = "INV-001",
            Price = 50.00m,
            StockQuantity = 5,
            CategoryId = 999
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

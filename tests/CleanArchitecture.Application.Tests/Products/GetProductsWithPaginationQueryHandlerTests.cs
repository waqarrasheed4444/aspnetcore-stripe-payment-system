using CleanArchitecture.Application.Products.Queries.GetProductsWithPagination;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Tests.Products;

public class GetProductsWithPaginationQueryHandlerTests
{
    private readonly ApplicationDbContext _context;
    private readonly GetProductsWithPaginationQueryHandler _handler;

    public GetProductsWithPaginationQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _handler = new GetProductsWithPaginationQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedProducts()
    {
        // Arrange
        var category = new Category { Name = "Hardware", Description = "Computer Hardware" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        _context.Products.AddRange(
            new Product { Name = "SSD 1TB", SKU = "SSD-001", Price = 90.00m, CategoryId = category.Id },
            new Product { Name = "RAM 16GB", SKU = "RAM-002", Price = 60.00m, CategoryId = category.Id },
            new Product { Name = "GPU RTX 4070", SKU = "GPU-003", Price = 599.00m, CategoryId = category.Id }
        );
        await _context.SaveChangesAsync();

        var query = new GetProductsWithPaginationQuery
        {
            PageNumber = 1,
            PageSize = 2
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Count.Should().Be(2);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
    }
}

using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Persistence;

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        if (!await _context.Categories.AnyAsync())
        {
            var electronics = new Category
            {
                Name = "Electronics",
                Description = "Gadgets, devices, and electronic accessories"
            };

            var office = new Category
            {
                Name = "Office Supplies",
                Description = "Desks, chairs, stationary, and ergonomic tools"
            };

            _context.Categories.AddRange(electronics, office);
            await _context.SaveChangesAsync();

            if (!await _context.Products.AnyAsync())
            {
                _context.Products.AddRange(
                    new Product
                    {
                        Name = "Wireless Noise-Canceling Headphones",
                        SKU = "ELEC-HEAD-001",
                        Description = "High-fidelity Bluetooth headphones with active noise cancellation.",
                        Price = 249.99m,
                        StockQuantity = 45,
                        Status = ProductStatus.Active,
                        Category = electronics
                    },
                    new Product
                    {
                        Name = "Ergonomic Mesh Office Chair",
                        SKU = "OFF-CHAIR-002",
                        Description = "Adjustable lumbar support with breathable mesh backrest.",
                        Price = 320.00m,
                        StockQuantity = 12,
                        Status = ProductStatus.Active,
                        Category = office
                    },
                    new Product
                    {
                        Name = "Mechanical RGB Keyboard",
                        SKU = "ELEC-KEY-003",
                        Description = "Hot-swappable tactile switches with customizable backlight.",
                        Price = 119.50m,
                        StockQuantity = 30,
                        Status = ProductStatus.Active,
                        Category = electronics
                    }
                );

                await _context.SaveChangesAsync();
            }
        }
    }
}

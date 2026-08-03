using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.WebApi.Controllers;

public class CategoriesController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;

    public CategoriesController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all product categories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories(CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}

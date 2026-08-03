using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Products.Commands.CreateProduct;
using CleanArchitecture.Application.Products.Commands.DeleteProduct;
using CleanArchitecture.Application.Products.Commands.UpdateProduct;
using CleanArchitecture.Application.Products.Dtos;
using CleanArchitecture.Application.Products.Queries.GetProductById;
using CleanArchitecture.Application.Products.Queries.GetProductsWithPagination;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebApi.Controllers;

public class ProductsController : ApiControllerBase
{
    /// <summary>
    /// Gets a paginated list of products with optional search and category filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<ProductDto>>> GetProducts([FromQuery] GetProductsWithPaginationQuery query)
    {
        return await Mediator.Send(query);
    }

    /// <summary>
    /// Gets a single product by Id.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        return await Mediator.Send(new GetProductByIdQuery(id));
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Create(CreateProductCommand command)
    {
        var productId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetProduct), new { id = productId }, productId);
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateProductCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new ProblemDetails { Title = "Mismatched Id", Detail = "URL Id does not match payload Id." });
        }

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Deletes a product by Id.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await Mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }
}

using CleanArchitecture.Application.Orders.Commands.CreateOrder;
using CleanArchitecture.Application.Orders.Dtos;
using CleanArchitecture.Application.Orders.Queries.GetOrderById;
using CleanArchitecture.Application.Orders.Queries.GetOrdersByUserId;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebApi.Controllers;

/// <summary>
/// Manages customer orders. Orders must be created here to ensure prices
/// are always retrieved from the database — never trusted from the client.
/// </summary>
public class OrdersController : ApiControllerBase
{
    /// <summary>
    /// Creates a new order from the product catalogue.
    /// Prices are always sourced from the database.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> Create([FromBody] CreateOrderCommand command)
    {
        var orderId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = orderId }, orderId);
    }

    /// <summary>
    /// Gets a single order by its ID, including items and payment status.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        return await Mediator.Send(new GetOrderByIdQuery(id));
    }

    /// <summary>
    /// Gets all orders for a given user.
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderDto>>> GetByUserId(string userId)
    {
        return await Mediator.Send(new GetOrdersByUserIdQuery(userId));
    }
}

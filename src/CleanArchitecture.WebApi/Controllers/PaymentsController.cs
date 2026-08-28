using CleanArchitecture.Application.Payments.Commands.CreatePaymentCheckout;
using CleanArchitecture.Application.Payments.Commands.RefundPayment;
using CleanArchitecture.Application.Payments.Dtos;
using CleanArchitecture.Application.Payments.Queries.GetPaymentById;
using CleanArchitecture.Application.Payments.Queries.GetPaymentsByUserId;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebApi.Controllers;

/// <summary>
/// Stripe payment operations: checkout sessions, payment history, and refunds.
/// Prices are always sourced from the database — never trusted from the client.
/// </summary>
public class PaymentsController : ApiControllerBase
{
    /// <summary>
    /// Creates a Stripe Checkout Session for an existing order.
    /// Returns a URL to redirect the user to Stripe-hosted checkout.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(CheckoutSessionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckoutSessionResponseDto>> CreateCheckout([FromBody] CreatePaymentCheckoutCommand command)
    {
        return await Mediator.Send(command);
    }

    /// <summary>
    /// Issues a full or partial refund for a completed payment.
    /// Amount defaults to the full refundable amount if not specified.
    /// </summary>
    [HttpPost("refund")]
    [ProducesResponseType(typeof(RefundPaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RefundPaymentResponseDto>> Refund([FromBody] RefundPaymentCommand command)
    {
        return await Mediator.Send(command);
    }

    /// <summary>
    /// Gets a single payment by its internal ID, including all transactions.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> GetById(int id)
    {
        return await Mediator.Send(new GetPaymentByIdQuery(id));
    }

    /// <summary>
    /// Gets all payments for a given user, ordered by most recent.
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentDto>>> GetByUserId(string userId)
    {
        return await Mediator.Send(new GetPaymentsByUserIdQuery(userId));
    }
}

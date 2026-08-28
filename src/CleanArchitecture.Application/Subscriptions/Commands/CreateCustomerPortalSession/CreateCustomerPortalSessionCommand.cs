using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Subscriptions.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Subscriptions.Commands.CreateCustomerPortalSession;

public class CreateCustomerPortalSessionCommand : IRequest<CustomerPortalResponseDto>
{
    public string UserId { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
}

public class CreateCustomerPortalSessionCommandValidator : AbstractValidator<CreateCustomerPortalSessionCommand>
{
    public CreateCustomerPortalSessionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.ReturnUrl).NotEmpty().WithMessage("ReturnUrl is required.");
    }
}

public class CreateCustomerPortalSessionCommandHandler : IRequestHandler<CreateCustomerPortalSessionCommand, CustomerPortalResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IStripeSubscriptionService _stripeSubscriptionService;

    public CreateCustomerPortalSessionCommandHandler(
        IApplicationDbContext context,
        IStripeSubscriptionService stripeSubscriptionService)
    {
        _context = context;
        _stripeSubscriptionService = stripeSubscriptionService;
    }

    public async Task<CustomerPortalResponseDto> Handle(CreateCustomerPortalSessionCommand request, CancellationToken cancellationToken)
    {
        var stripeCustomer = await _context.StripeCustomers.FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);
        if (stripeCustomer == null || string.IsNullOrEmpty(stripeCustomer.StripeCustomerId))
        {
            throw new NotFoundException($"No Stripe customer found for User ID {request.UserId}.");
        }

        var portalUrl = await _stripeSubscriptionService.CreateCustomerPortalSessionAsync(
            stripeCustomer.StripeCustomerId,
            request.ReturnUrl,
            cancellationToken);

        return new CustomerPortalResponseDto
        {
            PortalUrl = portalUrl
        };
    }
}

using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

public class StripeCustomer : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StripeCustomerId { get; set; } = string.Empty;
}

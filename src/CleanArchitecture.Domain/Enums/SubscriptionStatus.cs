namespace CleanArchitecture.Domain.Enums;

public enum SubscriptionStatus
{
    Incomplete = 1,
    IncompleteExpired = 2,
    Trialing = 3,
    Active = 4,
    PastDue = 5,
    Canceled = 6,
    Unpaid = 7
}

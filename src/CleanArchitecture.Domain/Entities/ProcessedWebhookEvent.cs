using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

public class ProcessedWebhookEvent : BaseEntity
{
    public string StripeEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.StripeEventId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => e.StripeEventId).IsUnique();
    }
}

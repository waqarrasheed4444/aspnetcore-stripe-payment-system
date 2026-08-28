using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.StripeCustomerId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.StripeSubscriptionId)
            .HasMaxLength(100);

        builder.Property(s => s.StripePriceId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.PlanName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => s.StripeSubscriptionId);
        builder.HasIndex(s => s.UserId);
    }
}

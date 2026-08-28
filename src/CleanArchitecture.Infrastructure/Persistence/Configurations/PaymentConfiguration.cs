using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.StripeCustomerId)
            .HasMaxLength(100);

        builder.Property(p => p.StripePaymentIntentId)
            .HasMaxLength(100);

        builder.Property(p => p.StripeCheckoutSessionId)
            .HasMaxLength(150);

        builder.Property(p => p.StripeSubscriptionId)
            .HasMaxLength(100);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.AmountRefunded)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.FailureMessage)
            .HasMaxLength(1000);

        builder.HasMany(p => p.Transactions)
            .WithOne(t => t.Payment)
            .HasForeignKey(t => t.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.StripePaymentIntentId);
        builder.HasIndex(p => p.StripeCheckoutSessionId);
        builder.HasIndex(p => p.UserId);
    }
}

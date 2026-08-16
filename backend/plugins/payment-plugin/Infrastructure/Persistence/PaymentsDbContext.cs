using Microsoft.EntityFrameworkCore;
using PaymentPlugin.Domain.Entities;

namespace PaymentPlugin.Infrastructure.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<ProductPriceProjection> ProductPrices => Set<ProductPriceProjection>();
    public DbSet<StripeWebhookRecord> StripeWebhookEvents => Set<StripeWebhookRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<PaymentRecord>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Id).HasMaxLength(64);
            entity.Property(payment => payment.UserId).HasMaxLength(64).IsRequired();
            entity.Property(payment => payment.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(payment => payment.CurrencyCode).HasMaxLength(8).IsRequired();
            entity.Property(payment => payment.Status).HasMaxLength(32).IsRequired();
            entity.Property(payment => payment.PaymentMethod).HasMaxLength(32).IsRequired();
            entity.Property(payment => payment.ProviderReference).HasMaxLength(128);
            entity.Property(payment => payment.FailureReason).HasMaxLength(512);
            entity.Property(payment => payment.Amount).HasPrecision(12, 2);
            entity.HasIndex(payment => payment.CorrelationId).IsUnique();
            entity.HasIndex(payment => payment.UserId);
        });

        builder.Entity<ProductPriceProjection>(entity =>
        {
            entity.ToTable("ProductPriceProjection");
            entity.HasKey(price => price.ProductId);
            entity.Property(price => price.ProductId).HasMaxLength(64);
            entity.Property(price => price.CurrencyCode).HasMaxLength(8).IsRequired();
            entity.Property(price => price.Price).HasPrecision(12, 2);
        });

        builder.Entity<StripeWebhookRecord>(entity =>
        {
            entity.ToTable("StripeWebhookEvents");
            entity.HasKey(webhook => webhook.EventId);
            entity.Property(webhook => webhook.EventId).HasMaxLength(128);
            entity.Property(webhook => webhook.EventType).HasMaxLength(128).IsRequired();
            entity.Property(webhook => webhook.ProviderReference).HasMaxLength(128);
            entity.Property(webhook => webhook.CustomerId).HasMaxLength(128);
            entity.Property(webhook => webhook.CustomerEmail).HasMaxLength(320);
            entity.Property(webhook => webhook.Amount).HasPrecision(12, 2);
            entity.Property(webhook => webhook.CurrencyCode).HasMaxLength(8);
            entity.Property(webhook => webhook.Status).HasMaxLength(64);
            entity.HasIndex(webhook => webhook.ReceivedAtUtc);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using PaymentPlugin.Domain.Entities;

namespace PaymentPlugin.Infrastructure.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<ProductPriceProjection> ProductPrices => Set<ProductPriceProjection>();

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
    }
}

using Microsoft.EntityFrameworkCore;
using OrderPlugin.Domain.Entities;

namespace OrderPlugin.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Id).HasMaxLength(64);
            entity.Property(order => order.UserId).HasMaxLength(64).IsRequired();
            entity.Property(order => order.CartId).HasMaxLength(64).IsRequired();
            entity.Property(order => order.PaymentId).HasMaxLength(64).IsRequired();
            entity.Property(order => order.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(order => order.Status).HasMaxLength(32).IsRequired();
            entity.Property(order => order.CurrencyCode).HasMaxLength(8).IsRequired();
            entity.Property(order => order.PaymentMethod).HasMaxLength(32).IsRequired();
            entity.Property(order => order.ProviderReference).HasMaxLength(128).IsRequired();
            entity.Property(order => order.TotalAmount).HasPrecision(12, 2);
            entity.HasIndex(order => order.PaymentId).IsUnique();
            entity.HasIndex(order => order.CorrelationId).IsUnique();
            entity.HasIndex(order => new { order.UserId, order.CreatedAtUtc });
            entity.HasMany(order => order.Items).WithOne().HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(64);
            entity.Property(item => item.OrderId).HasMaxLength(64).IsRequired();
            entity.Property(item => item.ProductId).HasMaxLength(64).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(12, 2);
            entity.Property(item => item.LineTotal).HasPrecision(12, 2);
        });
    }
}

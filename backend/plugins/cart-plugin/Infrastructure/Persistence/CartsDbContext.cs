using CartPlugin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CartPlugin.Infrastructure.Persistence;

public sealed class CartsDbContext(DbContextOptions<CartsDbContext> options) : DbContext(options)
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(cart => cart.Id);
            entity.Property(cart => cart.Id).HasMaxLength(64);
            entity.Property(cart => cart.UserId).HasMaxLength(64).IsRequired();
            entity.HasIndex(cart => cart.UserId).IsUnique();
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(64);
            entity.Property(item => item.CartId).HasMaxLength(64).IsRequired();
            entity.Property(item => item.ProductId).HasMaxLength(64).IsRequired();
            entity.HasIndex(item => new { item.CartId, item.ProductId }).IsUnique();

            entity.HasOne<Cart>()
                .WithMany(cart => cart.Items)
                .HasForeignKey(item => item.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

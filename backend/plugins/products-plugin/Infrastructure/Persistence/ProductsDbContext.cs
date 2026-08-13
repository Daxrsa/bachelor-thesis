using Microsoft.EntityFrameworkCore;
using ProductsPlugin.Domain.Entities;

namespace ProductsPlugin.Infrastructure.Persistence;

public sealed class ProductsDbContext(DbContextOptions<ProductsDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).HasMaxLength(64);
            entity.Property(product => product.Code).HasMaxLength(64);
            entity.Property(product => product.Name).HasMaxLength(256).IsRequired();
            entity.Property(product => product.InventoryStatus).HasMaxLength(32).IsRequired();
            entity.Property(product => product.Category).HasMaxLength(128);
            entity.Property(product => product.ImageFileName).HasMaxLength(256);
            entity.Property(product => product.ImageUrl).HasMaxLength(1024);
            entity.Property(product => product.Image).HasMaxLength(256);
            entity.Property(product => product.Price).HasPrecision(12, 2);
            entity.HasIndex(product => product.InventoryStatus);
            entity.HasIndex(product => product.Price);
        });
    }
}
using ECommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PluginInstallation> PluginInstallations => Set<PluginInstallation>();
    public DbSet<MarketplaceListing> MarketplaceListings => Set<MarketplaceListing>();
    public DbSet<PublisherRequest> PublisherRequests => Set<PublisherRequest>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(320).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasMaxLength(32);
        });

        b.Entity<PluginInstallation>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.PluginId).IsUnique();
            e.Property(p => p.PluginId).HasMaxLength(128).IsRequired();
            e.Property(p => p.Version).HasMaxLength(64).IsRequired();
            e.Property(p => p.Image).IsRequired();
            e.Property(p => p.ContainerName).HasMaxLength(256).IsRequired();
            e.Property(p => p.State).HasConversion<string>().HasMaxLength(32);
        });

        b.Entity<MarketplaceListing>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.PluginId).IsUnique();
            e.Property(l => l.PluginId).HasMaxLength(128).IsRequired();
            e.Property(l => l.Name).HasMaxLength(256).IsRequired();
            e.Property(l => l.Version).HasMaxLength(64).IsRequired();
            e.Property(l => l.Description).IsRequired();
            e.Property(l => l.Publisher).HasMaxLength(256).IsRequired();
            e.Property(l => l.Image).IsRequired();
            e.Property(l => l.HealthEndpoint).HasMaxLength(256);
            e.Property(l => l.HostApi).HasMaxLength(64);
            e.HasIndex(l => l.PublishedByUserId);
        });

        b.Entity<PublisherRequest>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.UserId);
            e.Property(r => r.Status).HasMaxLength(32).IsRequired();
            e.Property(r => r.Message).HasMaxLength(1000);
        });
    }
}

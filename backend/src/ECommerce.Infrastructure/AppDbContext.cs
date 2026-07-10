using ECommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PluginInstallation> PluginInstallations => Set<PluginInstallation>();

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
    }
}

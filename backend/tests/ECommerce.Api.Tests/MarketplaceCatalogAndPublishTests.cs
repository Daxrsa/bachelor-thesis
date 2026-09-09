using System.Text.Json;
using ECommerce.Api.Plugins;
using ECommerce.Core.Entities;
using ECommerce.Infrastructure;
using ECommerce.PluginContracts;
using ECommerce.PluginRuntime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.Api.Tests;

public sealed class MarketplaceCatalogAndPublishTests
{
    [Fact]
    public async Task Composite_OfficialWinsOnCollision()
    {
        var official = Path.GetTempFileName();
        var third = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(official, JsonSerializer.Serialize(new[]
            {
                CreateManifest("hello-plugin", "Official", "0.1.0")
            }));
            await File.WriteAllTextAsync(third, JsonSerializer.Serialize(new[]
            {
                CreateManifest("hello-plugin", "Hijack", "9.9.9")
            }));

            var catalog = new CompositeMarketplaceCatalog(
                [
                    new BundledMarketplaceSource(official, "official"),
                    new BundledMarketplaceSource(third, "third"),
                ],
                NullLogger<CompositeMarketplaceCatalog>.Instance);

            var hello = await catalog.GetAsync("hello-plugin");
            Assert.NotNull(hello);
            Assert.Equal("Official", hello!.Name);
            Assert.Equal("0.1.0", hello.Version);
        }
        finally
        {
            File.Delete(official);
            File.Delete(third);
        }
    }

    [Fact]
    public async Task Composite_FailingSourceIsSkipped()
    {
        var good = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(good, JsonSerializer.Serialize(new[]
            {
                CreateManifest("hello-plugin", "Ok", "0.1.0")
            }));

            var catalog = new CompositeMarketplaceCatalog(
                [
                    new ThrowingSource(),
                    new BundledMarketplaceSource(good, "official"),
                ],
                NullLogger<CompositeMarketplaceCatalog>.Instance);

            var list = await catalog.ListAsync();
            Assert.Single(list);
            Assert.Equal("hello-plugin", list[0].Id);
        }
        finally
        {
            File.Delete(good);
        }
    }

    [Fact]
    public async Task Publish_RejectsOfficialPluginId()
    {
        var official = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(official, JsonSerializer.Serialize(new[]
            {
                CreateManifest("hello-plugin", "Official", "0.1.0")
            }));

            await using var provider = BuildDb();
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = new MarketplacePublishService(db, new BundledMarketplaceSource(official, "official"));

            var ex = await Assert.ThrowsAsync<MarketplaceException>(() =>
                svc.PublishAsync(CreateManifest("hello-plugin", "X", "1.0.0"), Guid.NewGuid(), CancellationToken.None));

            Assert.Equal(409, ex.StatusCode);
            Assert.Contains("reserved", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(official);
        }
    }

    [Fact]
    public async Task Publish_UpsertsForSameOwner_AndRejectsOtherOwner()
    {
        var official = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(official, "[]");
            await using var provider = BuildDb();
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = new MarketplacePublishService(db, new BundledMarketplaceSource(official, "official"));

            var owner = Guid.NewGuid();
            var other = Guid.NewGuid();
            var first = await svc.PublishAsync(CreateManifest("reviews-plugin", "Reviews", "1.0.0"), owner, CancellationToken.None);
            Assert.Equal("1.0.0", first.Version);

            var updated = await svc.PublishAsync(CreateManifest("reviews-plugin", "Reviews", "1.1.0"), owner, CancellationToken.None);
            Assert.Equal("1.1.0", updated.Version);
            Assert.Equal(1, await db.MarketplaceListings.CountAsync());

            var conflict = await Assert.ThrowsAsync<MarketplaceException>(() =>
                svc.PublishAsync(CreateManifest("reviews-plugin", "Reviews", "2.0.0"), other, CancellationToken.None));
            Assert.Equal(409, conflict.StatusCode);
            Assert.Contains("another author", conflict.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(official);
        }
    }

    [Fact]
    public async Task DatabaseSource_AppearsInCompositeList()
    {
        var official = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(official, JsonSerializer.Serialize(new[]
            {
                CreateManifest("hello-plugin", "Official", "0.1.0")
            }));

            await using var provider = BuildDb();
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var listing = new MarketplaceListing
            {
                PluginId = "reviews-plugin",
                Name = "Reviews",
                Version = "1.0.0",
                Description = "Community reviews",
                Publisher = "acme",
                Image = "localhost:5000/ecommerce/reviews-plugin:1.0.0",
                PublishedByUserId = Guid.NewGuid()
            };
            MarketplaceListingMapper.ApplyManifest(listing, CreateManifest("reviews-plugin", "Reviews", "1.0.0"));
            db.MarketplaceListings.Add(listing);
            await db.SaveChangesAsync();

            // Read via a fresh scope (same as DatabaseMarketplaceSource does at runtime).
            await using var readScope = provider.CreateAsyncScope();
            var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await readDb.MarketplaceListings.CountAsync());

            var dbSource = new DatabaseMarketplaceSource(provider.GetRequiredService<IServiceScopeFactory>());
            var fromDb = await dbSource.ListAsync();
            Assert.Contains(fromDb, m => m.Id == "reviews-plugin");

            var catalog = new CompositeMarketplaceCatalog(
                [
                    new BundledMarketplaceSource(official, "official"),
                    dbSource,
                ],
                NullLogger<CompositeMarketplaceCatalog>.Instance);

            var ids = (await catalog.ListAsync()).Select(m => m.Id).ToList();
            Assert.Contains("hello-plugin", ids);
            Assert.Contains("reviews-plugin", ids);
        }
        finally
        {
            File.Delete(official);
        }
    }

    [Fact]
    public async Task Publish_InvalidId_IsBadRequest()
    {
        var official = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(official, "[]");
            await using var provider = BuildDb();
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = new MarketplacePublishService(db, new BundledMarketplaceSource(official, "official"));

            var ex = await Assert.ThrowsAsync<MarketplaceException>(() =>
                svc.PublishAsync(CreateManifest("NotValid", "X", "1.0.0"), Guid.NewGuid(), CancellationToken.None));

            Assert.Equal(400, ex.StatusCode);
        }
        finally
        {
            File.Delete(official);
        }
    }

    [Fact]
    public async Task ListMine_ReturnsOnlyOwnerListings()
    {
        var official = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(official, "[]");
            await using var provider = BuildDb();
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = new MarketplacePublishService(db, new BundledMarketplaceSource(official, "official"));

            var owner = Guid.NewGuid();
            var other = Guid.NewGuid();
            await svc.PublishAsync(CreateManifest("reviews-plugin", "Reviews", "1.0.0"), owner, CancellationToken.None);
            await svc.PublishAsync(CreateManifest("wishlist-plugin", "Wishlist", "1.0.0"), other, CancellationToken.None);

            var mine = await svc.ListMineAsync(owner, CancellationToken.None);
            Assert.Single(mine);
            Assert.Equal("reviews-plugin", mine[0].Manifest.Id);
            Assert.False(mine[0].Installed);
        }
        finally
        {
            File.Delete(official);
        }
    }

    [Fact]
    public async Task PublisherRequest_Approve_SetsPublisherRole()
    {
        await using var provider = BuildDb();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new User
        {
            Email = "author@example.com",
            PasswordHash = "x",
            Role = "user"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = new PublisherRequestService(db);
        var created = await svc.RequestAsync(user.Id, "I want to publish reviews", CancellationToken.None);
        Assert.Equal("pending", created.Status);

        var duplicate = await Assert.ThrowsAsync<MarketplaceException>(() =>
            svc.RequestAsync(user.Id, null, CancellationToken.None));
        Assert.Equal(409, duplicate.StatusCode);

        var adminId = Guid.NewGuid();
        var approved = await svc.ApproveAsync(created.Id, adminId, CancellationToken.None);
        Assert.Equal("approved", approved.Status);

        var reloaded = await db.Users.FirstAsync(u => u.Id == user.Id);
        Assert.Equal("publisher", reloaded.Role);
    }

    private static ServiceProvider BuildDb()
    {
        var databaseName = Guid.NewGuid().ToString();
        var root = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName, root));
        return services.BuildServiceProvider();
    }

    private static PluginManifest CreateManifest(string id, string name, string version) => new()
    {
        Id = id,
        Name = name,
        Version = version,
        Description = "Test plugin",
        Publisher = "Tests",
        Image = $"example/{id}:{version}"
    };

    private sealed class ThrowingSource : IMarketplaceSource
    {
        public string Name => "throwing";
        public Task<IReadOnlyList<PluginManifest>> ListAsync(CancellationToken ct = default)
            => throw new InvalidOperationException("boom");
    }
}

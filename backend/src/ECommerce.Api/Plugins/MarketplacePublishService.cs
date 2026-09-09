using ECommerce.Core.Entities;
using ECommerce.Infrastructure;
using ECommerce.PluginContracts;
using ECommerce.PluginRuntime;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Plugins;

public sealed record MarketplaceListingDto(
    PluginManifest Manifest,
    Guid PublishedByUserId,
    DateTime PublishedAt,
    DateTime UpdatedAt,
    bool Installed,
    string? InstallState);

public interface IMarketplacePublishService
{
    Task<IReadOnlyList<PluginManifest>> ListListingsAsync(CancellationToken ct);
    Task<IReadOnlyList<MarketplaceListingDto>> ListMineAsync(Guid userId, CancellationToken ct);
    Task<MarketplaceListingDto> GetOwnedAsync(string pluginId, Guid userId, string role, CancellationToken ct);
    Task<MarketplaceListing> PublishAsync(PluginManifest manifest, Guid userId, CancellationToken ct);
    Task<MarketplaceListing> UpdateAsync(string pluginId, PluginManifest manifest, Guid userId, CancellationToken ct);
    Task DeleteAsync(string pluginId, Guid userId, string role, CancellationToken ct);
}

public sealed class MarketplacePublishService(
    AppDbContext db,
    BundledMarketplaceSource officialSource) : IMarketplacePublishService
{
    public async Task<IReadOnlyList<PluginManifest>> ListListingsAsync(CancellationToken ct)
    {
        var listings = await db.MarketplaceListings.AsNoTracking().OrderBy(l => l.PluginId).ToListAsync(ct);
        return listings.Select(MarketplaceListingMapper.ToManifest).ToList();
    }

    public async Task<IReadOnlyList<MarketplaceListingDto>> ListMineAsync(Guid userId, CancellationToken ct)
    {
        var listings = await db.MarketplaceListings
            .AsNoTracking()
            .Where(l => l.PublishedByUserId == userId)
            .OrderBy(l => l.PluginId)
            .ToListAsync(ct);

        return await ToDtosAsync(listings, ct);
    }

    public async Task<MarketplaceListingDto> GetOwnedAsync(string pluginId, Guid userId, string role, CancellationToken ct)
    {
        var listing = await db.MarketplaceListings.AsNoTracking()
                          .FirstOrDefaultAsync(l => l.PluginId == pluginId, ct)
                      ?? throw new MarketplaceException(StatusCodes.Status404NotFound, $"Listing '{pluginId}' not found");

        var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && listing.PublishedByUserId != userId)
            throw new MarketplaceException(StatusCodes.Status404NotFound, $"Listing '{pluginId}' not found");

        var dtos = await ToDtosAsync([listing], ct);
        return dtos[0];
    }

    public async Task<MarketplaceListing> PublishAsync(PluginManifest manifest, Guid userId, CancellationToken ct)
    {
        ValidateManifest(manifest);

        if (await IsOfficialPluginIdAsync(manifest.Id, ct))
            throw new MarketplaceException(
                StatusCodes.Status409Conflict,
                $"Plugin id '{manifest.Id}' is reserved by the official catalog");

        var existing = await db.MarketplaceListings.FirstOrDefaultAsync(l => l.PluginId == manifest.Id, ct);
        if (existing is not null)
        {
            if (existing.PublishedByUserId != userId)
                throw new MarketplaceException(
                    StatusCodes.Status409Conflict,
                    $"Plugin '{manifest.Id}' is already published by another author");

            MarketplaceListingMapper.ApplyManifest(existing, manifest);
            await db.SaveChangesAsync(ct);
            return existing;
        }

        var listing = new MarketplaceListing
        {
            PluginId = manifest.Id,
            Name = manifest.Name,
            Version = manifest.Version,
            Description = manifest.Description,
            Publisher = manifest.Publisher,
            Image = manifest.Image,
            PublishedByUserId = userId
        };
        MarketplaceListingMapper.ApplyManifest(listing, manifest);
        listing.PublishedAt = DateTime.UtcNow;
        db.MarketplaceListings.Add(listing);
        await db.SaveChangesAsync(ct);
        return listing;
    }

    public async Task<MarketplaceListing> UpdateAsync(
        string pluginId,
        PluginManifest manifest,
        Guid userId,
        CancellationToken ct)
    {
        if (!string.Equals(pluginId, manifest.Id, StringComparison.Ordinal))
            throw new MarketplaceException(StatusCodes.Status400BadRequest, "Route plugin id must match manifest.id");

        ValidateManifest(manifest);

        if (await IsOfficialPluginIdAsync(manifest.Id, ct))
            throw new MarketplaceException(
                StatusCodes.Status409Conflict,
                $"Plugin id '{manifest.Id}' is reserved by the official catalog");

        var existing = await db.MarketplaceListings.FirstOrDefaultAsync(l => l.PluginId == pluginId, ct)
                       ?? throw new MarketplaceException(StatusCodes.Status404NotFound, $"Listing '{pluginId}' not found");

        if (existing.PublishedByUserId != userId)
            throw new MarketplaceException(StatusCodes.Status403Forbidden, $"You do not own listing '{pluginId}'");

        MarketplaceListingMapper.ApplyManifest(existing, manifest);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(string pluginId, Guid userId, string role, CancellationToken ct)
    {
        var existing = await db.MarketplaceListings.FirstOrDefaultAsync(l => l.PluginId == pluginId, ct);
        if (existing is null)
            return;

        var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && existing.PublishedByUserId != userId)
            throw new MarketplaceException(StatusCodes.Status403Forbidden, $"You do not own listing '{pluginId}'");

        db.MarketplaceListings.Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<MarketplaceListingDto>> ToDtosAsync(
        IReadOnlyList<MarketplaceListing> listings,
        CancellationToken ct)
    {
        var ids = listings.Select(l => l.PluginId).ToList();
        var installs = await db.PluginInstallations.AsNoTracking()
            .Where(p => ids.Contains(p.PluginId))
            .ToDictionaryAsync(p => p.PluginId, ct);

        return listings.Select(listing =>
        {
            installs.TryGetValue(listing.PluginId, out var install);
            return new MarketplaceListingDto(
                MarketplaceListingMapper.ToManifest(listing),
                listing.PublishedByUserId,
                listing.PublishedAt,
                listing.UpdatedAt,
                install is not null,
                install?.State.ToString());
        }).ToList();
    }

    private async Task<bool> IsOfficialPluginIdAsync(string pluginId, CancellationToken ct)
    {
        var official = await officialSource.ListAsync(ct);
        return official.Any(m => m.Id == pluginId);
    }

    private static void ValidateManifest(PluginManifest manifest)
    {
        if (!PluginIdRules.IsValid(manifest.Id))
            throw new MarketplaceException(
                StatusCodes.Status400BadRequest,
                "Invalid plugin id. Expected lowercase kebab-case ending with '-plugin' (e.g. reviews-plugin).");

        if (string.IsNullOrWhiteSpace(manifest.Name))
            throw new MarketplaceException(StatusCodes.Status400BadRequest, "Name is required");
        if (string.IsNullOrWhiteSpace(manifest.Version))
            throw new MarketplaceException(StatusCodes.Status400BadRequest, "Version is required");
        if (string.IsNullOrWhiteSpace(manifest.Description))
            throw new MarketplaceException(StatusCodes.Status400BadRequest, "Description is required");
        if (string.IsNullOrWhiteSpace(manifest.Publisher))
            throw new MarketplaceException(StatusCodes.Status400BadRequest, "Publisher is required");
        if (string.IsNullOrWhiteSpace(manifest.Image))
            throw new MarketplaceException(StatusCodes.Status400BadRequest, "Image is required");
        if (manifest.ContainerPort <= 0)
            throw new MarketplaceException(StatusCodes.Status400BadRequest, "ContainerPort must be positive");
        if (string.IsNullOrWhiteSpace(manifest.HealthEndpoint))
            throw new MarketplaceException(StatusCodes.Status400BadRequest, "HealthEndpoint is required");
    }
}

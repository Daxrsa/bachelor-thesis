using ECommerce.Core.Entities;
using ECommerce.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Plugins;

public sealed record PublisherRequestDto(
    Guid Id,
    Guid UserId,
    string Email,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt);

public interface IPublisherRequestService
{
    Task<PublisherRequestDto> RequestAsync(Guid userId, string? message, CancellationToken ct);
    Task<PublisherRequestDto?> GetMineAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<PublisherRequestDto>> ListPendingAsync(CancellationToken ct);
    Task<PublisherRequestDto> ApproveAsync(Guid requestId, Guid adminUserId, CancellationToken ct);
    Task<PublisherRequestDto> RejectAsync(Guid requestId, Guid adminUserId, CancellationToken ct);
}

public sealed class PublisherRequestService(AppDbContext db) : IPublisherRequestService
{
    public async Task<PublisherRequestDto> RequestAsync(Guid userId, string? message, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new MarketplaceException(StatusCodes.Status404NotFound, "User not found");

        if (user.Role is "publisher" or "admin")
            throw new MarketplaceException(StatusCodes.Status400BadRequest, "You already have publisher access");

        var pending = await db.PublisherRequests
            .AnyAsync(r => r.UserId == userId && r.Status == PublisherRequestStatus.Pending, ct);
        if (pending)
            throw new MarketplaceException(StatusCodes.Status409Conflict, "A publisher request is already pending");

        var request = new PublisherRequest
        {
            UserId = userId,
            Message = (message ?? string.Empty).Trim(),
            Status = PublisherRequestStatus.Pending
        };
        db.PublisherRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return ToDto(request, user.Email);
    }

    public async Task<PublisherRequestDto?> GetMineAsync(Guid userId, CancellationToken ct)
    {
        var request = await db.PublisherRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (request is null)
            return null;

        var email = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstAsync(ct);
        return ToDto(request, email);
    }

    public async Task<IReadOnlyList<PublisherRequestDto>> ListPendingAsync(CancellationToken ct)
    {
        var pending = await db.PublisherRequests.AsNoTracking()
            .Where(r => r.Status == PublisherRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

        var userIds = pending.Select(r => r.UserId).Distinct().ToList();
        var emails = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        return pending.Select(r => ToDto(r, emails.GetValueOrDefault(r.UserId, string.Empty))).ToList();
    }

    public async Task<PublisherRequestDto> ApproveAsync(Guid requestId, Guid adminUserId, CancellationToken ct)
    {
        var request = await db.PublisherRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
                      ?? throw new MarketplaceException(StatusCodes.Status404NotFound, "Request not found");
        if (request.Status != PublisherRequestStatus.Pending)
            throw new MarketplaceException(StatusCodes.Status409Conflict, "Request is no longer pending");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
                   ?? throw new MarketplaceException(StatusCodes.Status404NotFound, "User not found");

        user.Role = "publisher";
        request.Status = PublisherRequestStatus.Approved;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByUserId = adminUserId;
        await db.SaveChangesAsync(ct);
        return ToDto(request, user.Email);
    }

    public async Task<PublisherRequestDto> RejectAsync(Guid requestId, Guid adminUserId, CancellationToken ct)
    {
        var request = await db.PublisherRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
                      ?? throw new MarketplaceException(StatusCodes.Status404NotFound, "Request not found");
        if (request.Status != PublisherRequestStatus.Pending)
            throw new MarketplaceException(StatusCodes.Status409Conflict, "Request is no longer pending");

        var email = await db.Users.AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        request.Status = PublisherRequestStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByUserId = adminUserId;
        await db.SaveChangesAsync(ct);
        return ToDto(request, email);
    }

    private static PublisherRequestDto ToDto(PublisherRequest request, string email) =>
        new(request.Id, request.UserId, email, request.Message, request.Status, request.CreatedAt, request.ReviewedAt);
}

using CartPlugin.Application.Carts;
using CartPlugin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CartPlugin.Infrastructure.Persistence;

public sealed class EfCartRepository(CartsDbContext db) : ICartRepository
{
    public Task<Cart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        db.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.UserId == userId, cancellationToken);

    public Task AddAsync(Cart cart, CancellationToken cancellationToken) =>
        db.Carts.AddAsync(cart, cancellationToken).AsTask();

    public Task RemoveAsync(Cart cart, CancellationToken cancellationToken)
    {
        db.Carts.Remove(cart);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}

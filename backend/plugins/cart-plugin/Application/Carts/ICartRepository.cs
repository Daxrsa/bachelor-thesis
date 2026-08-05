using CartPlugin.Domain.Entities;

namespace CartPlugin.Application.Carts;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task AddAsync(Cart cart, CancellationToken cancellationToken);
    Task RemoveAsync(Cart cart, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

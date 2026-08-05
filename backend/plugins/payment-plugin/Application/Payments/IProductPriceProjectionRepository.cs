using PaymentPlugin.Domain.Entities;

namespace PaymentPlugin.Application.Payments;

public interface IProductPriceProjectionRepository
{
    Task<ProductPriceProjection?> GetByProductIdAsync(string productId, CancellationToken cancellationToken);
    Task UpsertAsync(ProductPriceProjection projection, CancellationToken cancellationToken);
    Task DeleteAsync(string productId, CancellationToken cancellationToken);
}

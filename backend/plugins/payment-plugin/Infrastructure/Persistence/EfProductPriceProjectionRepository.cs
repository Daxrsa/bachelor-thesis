using Microsoft.EntityFrameworkCore;
using PaymentPlugin.Application.Payments;
using PaymentPlugin.Domain.Entities;

namespace PaymentPlugin.Infrastructure.Persistence;

public sealed class EfProductPriceProjectionRepository(PaymentsDbContext db) : IProductPriceProjectionRepository
{
    public Task<ProductPriceProjection?> GetByProductIdAsync(string productId, CancellationToken cancellationToken) =>
        db.ProductPrices.FirstOrDefaultAsync(price => price.ProductId == productId, cancellationToken);

    public async Task UpsertAsync(ProductPriceProjection projection, CancellationToken cancellationToken)
    {
        var existing = await db.ProductPrices.FirstOrDefaultAsync(price => price.ProductId == projection.ProductId, cancellationToken);
        if (existing is null)
        {
            await db.ProductPrices.AddAsync(projection, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        existing.Price = projection.Price;
        existing.CurrencyCode = projection.CurrencyCode;
        existing.UpdatedAtUtc = projection.UpdatedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string productId, CancellationToken cancellationToken)
    {
        var existing = await db.ProductPrices.FirstOrDefaultAsync(price => price.ProductId == productId, cancellationToken);
        if (existing is null)
            return;

        db.ProductPrices.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
    }
}

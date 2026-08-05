using CartPlugin.Domain.Entities;
using ECommerce.IntegrationContracts.V1;

namespace CartPlugin.Application.Carts;

public interface ICartService
{
    Task<Cart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<Cart> HandleProductAddedToCartEventAsync(
        IntegrationEventEnvelope<ProductAddedToCartEvent> envelope,
        CancellationToken cancellationToken);
    Task<Cart?> HandleProductRemovedFromCartEventAsync(
        IntegrationEventEnvelope<ProductRemovedFromCartEvent> envelope,
        CancellationToken cancellationToken);
}

public sealed class CartService(ICartRepository repository) : ICartService
{
    public Task<Cart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        repository.GetByUserIdAsync(userId, cancellationToken);

    public async Task<Cart> HandleProductAddedToCartEventAsync(
        IntegrationEventEnvelope<ProductAddedToCartEvent> envelope,
        CancellationToken cancellationToken)
    {
        var userId = envelope.ResourceIds.UserId
            ?? throw new InvalidOperationException("ProductAddedToCartEvent requires resourceIds.userId");
        var productId = envelope.ResourceIds.ProductId
            ?? throw new InvalidOperationException("ProductAddedToCartEvent requires resourceIds.productId");

        var cart = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null)
        {
            cart = new Cart
            {
                Id = envelope.ResourceIds.CartId ?? Guid.NewGuid().ToString("N"),
                UserId = userId,
                CreatedAtUtc = envelope.OccurredAtUtc,
                UpdatedAtUtc = envelope.OccurredAtUtc
            };

            await repository.AddAsync(cart, cancellationToken);
        }

        var item = cart.Items.FirstOrDefault(cartItem => cartItem.ProductId == productId);
        if (item is null)
        {
            item = new CartItem
            {
                Id = string.IsNullOrWhiteSpace(envelope.Payload.CartItemId)
                    ? Guid.NewGuid().ToString("N")
                    : envelope.Payload.CartItemId,
                CartId = cart.Id,
                ProductId = productId,
                Quantity = envelope.Payload.Quantity,
                CreatedAtUtc = envelope.Payload.AddedAtUtc,
                UpdatedAtUtc = envelope.Payload.AddedAtUtc
            };
            cart.Items.Add(item);
        }
        else
        {
            item.Quantity += envelope.Payload.Quantity;
            item.UpdatedAtUtc = envelope.Payload.AddedAtUtc;
        }

        cart.UpdatedAtUtc = envelope.Payload.AddedAtUtc;
        await repository.SaveChangesAsync(cancellationToken);
        return cart;
    }

    public async Task<Cart?> HandleProductRemovedFromCartEventAsync(
        IntegrationEventEnvelope<ProductRemovedFromCartEvent> envelope,
        CancellationToken cancellationToken)
    {
        var userId = envelope.ResourceIds.UserId
            ?? throw new InvalidOperationException("ProductRemovedFromCartEvent requires resourceIds.userId");
        var productId = envelope.ResourceIds.ProductId
            ?? throw new InvalidOperationException("ProductRemovedFromCartEvent requires resourceIds.productId");

        var cart = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null)
            return null;

        var item = cart.Items.FirstOrDefault(cartItem => cartItem.ProductId == productId);
        if (item is null)
            return cart;

        item.Quantity -= envelope.Payload.Quantity;
        if (item.Quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.UpdatedAtUtc = envelope.Payload.RemovedAtUtc;
        }

        cart.UpdatedAtUtc = envelope.Payload.RemovedAtUtc;
        await repository.SaveChangesAsync(cancellationToken);
        return cart;
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCartApplication(this IServiceCollection services) =>
        services.AddScoped<ICartService, CartService>();
}

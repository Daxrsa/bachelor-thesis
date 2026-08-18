using CartPlugin.Domain.Entities;
using ECommerce.IntegrationContracts.V1;

namespace CartPlugin.Application.Carts;

public interface ICartService
{
    Task<Cart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<int> CountItemsAsync(string userId, CancellationToken cancellationToken);
    Task<bool> DeleteByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<Cart> HandleProductAddedToCartEventAsync(
        IntegrationEventEnvelope<ProductAddedToCartEvent> envelope,
        CancellationToken cancellationToken);
    Task<Cart?> HandleProductRemovedFromCartEventAsync(
        IntegrationEventEnvelope<ProductRemovedFromCartEvent> envelope,
        CancellationToken cancellationToken);
    Task HandleOrderCreatedAsync(
        IntegrationEventEnvelope<OrderCreatedEvent> envelope,
        CancellationToken cancellationToken);
}

public sealed class CartService(ICartRepository repository) : ICartService
{
    public async Task<bool> DeleteByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Delete cart requires a valid userId");

        var cart = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null)
            return false;

        await repository.RemoveAsync(cart, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<Cart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        repository.GetByUserIdAsync(userId, cancellationToken);

    /// <summary>Total number of units across all cart lines; 0 when the user has no cart.</summary>
    public async Task<int> CountItemsAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Counting cart items requires a valid userId");

        var cart = await repository.GetByUserIdAsync(userId, cancellationToken);
        return cart?.Items.Sum(item => item.Quantity) ?? 0;
    }

    public async Task<Cart> HandleProductAddedToCartEventAsync(
        IntegrationEventEnvelope<ProductAddedToCartEvent> envelope,
        CancellationToken cancellationToken)
    {
        var userId = envelope.ResourceIds.UserId
            ?? throw new InvalidOperationException("ProductAddedToCartEvent requires resourceIds.userId");
        var productId = envelope.ResourceIds.ProductId;
        if (string.IsNullOrWhiteSpace(productId))
            throw new InvalidOperationException("ProductAddedToCartEvent requires resourceIds.productId");

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
        var productId = envelope.ResourceIds.ProductId;
        if (string.IsNullOrWhiteSpace(productId))
            throw new InvalidOperationException("ProductRemovedFromCartEvent requires resourceIds.productId");

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

    public async Task HandleOrderCreatedAsync(
        IntegrationEventEnvelope<OrderCreatedEvent> envelope,
        CancellationToken cancellationToken)
    {
        var userId = envelope.ResourceIds.UserId
            ?? throw new InvalidOperationException("OrderCreatedEvent requires resourceIds.userId");
        var cartId = envelope.ResourceIds.CartId
            ?? throw new InvalidOperationException("OrderCreatedEvent requires resourceIds.cartId");

        var cart = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null || !string.Equals(cart.Id, cartId, StringComparison.Ordinal))
            return;

        await repository.RemoveAsync(cart, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCartApplication(this IServiceCollection services) =>
        services.AddScoped<ICartService, CartService>();
}

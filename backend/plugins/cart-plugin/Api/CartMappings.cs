using CartPlugin.Api.Contracts;
using CartPlugin.Domain.Entities;

namespace CartPlugin.Api;

public static class CartMappings
{
    public static CartResponse ToResponse(this Cart cart) =>
        new(
            cart.Id,
            cart.UserId,
            cart.CreatedAtUtc,
            cart.UpdatedAtUtc,
            cart.Items
                .OrderBy(item => item.CreatedAtUtc)
                .Select(item => new CartItemResponse(
                    item.Id,
                    item.ProductId,
                    item.Quantity,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc))
                .ToArray());
}

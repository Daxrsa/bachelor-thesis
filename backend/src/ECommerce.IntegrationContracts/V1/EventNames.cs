namespace ECommerce.IntegrationContracts.V1;

public static class EventNames
{
    public const string ProductAddedToCart = "cart.product-added.v1";
    public const string ProductRemovedFromCart = "cart.product-removed.v1";
    public const string CartCheckoutRequested = "cart.checkout-requested.v1";
    public const string ProductUpserted = "product.upserted.v1";
    public const string ProductDeleted = "product.deleted.v1";
    public const string PaymentSucceeded = "payment.succeeded.v1";
    public const string PaymentFailed = "payment.failed.v1";
}

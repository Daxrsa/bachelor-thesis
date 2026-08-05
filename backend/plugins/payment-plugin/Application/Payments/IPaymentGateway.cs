namespace PaymentPlugin.Application.Payments;

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(
        string paymentId,
        decimal amount,
        string currencyCode,
        string paymentMethod,
        string userId,
        CancellationToken cancellationToken);
}

namespace PaymentPlugin.Infrastructure.Stripe;

public sealed class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string SuccessPaymentMethodToken { get; set; } = "pm_card_visa";
    public string FailurePaymentMethodToken { get; set; } = "pm_card_chargeDeclined";
}

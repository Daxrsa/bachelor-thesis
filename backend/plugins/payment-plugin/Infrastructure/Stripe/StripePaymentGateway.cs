using Microsoft.Extensions.Options;
using PaymentPlugin.Application.Payments;
using Stripe;

namespace PaymentPlugin.Infrastructure.Stripe;

public sealed class StripePaymentGateway(IOptions<StripeOptions> options) : IPaymentGateway
{
    private readonly StripeOptions _options = options.Value;

    public async Task<PaymentResult> ChargeAsync(
        string paymentId,
        decimal amount,
        string currencyCode,
        string paymentMethod,
        string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            return new PaymentResult(false, string.Empty, "Stripe secret key is not configured");

        StripeConfiguration.ApiKey = _options.SecretKey;
        var paymentIntentService = new PaymentIntentService();

        var isForcedFailure = string.Equals(paymentMethod, "declined", StringComparison.OrdinalIgnoreCase);
        var token = isForcedFailure ? _options.FailurePaymentMethodToken : _options.SuccessPaymentMethodToken;

        try
        {
            var amountInCents = decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
            var intent = await paymentIntentService.CreateAsync(
                new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = currencyCode.ToLowerInvariant(),
                    Confirm = true,
                    PaymentMethod = token,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    },
                    Description = $"Payment {paymentId} for user {userId}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["paymentId"] = paymentId,
                        ["userId"] = userId
                    }
                },
                new RequestOptions(),
                cancellationToken);

            if (intent.Status is "succeeded" or "requires_capture")
                return new PaymentResult(true, intent.Id, null);

            return new PaymentResult(false, intent.Id, $"Stripe intent status: {intent.Status}");
        }
        catch (StripeException ex)
        {
            return new PaymentResult(false, ex.StripeError?.PaymentIntent?.Id ?? string.Empty, ex.Message);
        }
    }
}

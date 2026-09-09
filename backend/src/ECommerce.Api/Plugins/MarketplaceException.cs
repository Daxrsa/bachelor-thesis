namespace ECommerce.Api.Plugins;

public sealed class MarketplaceException : Exception
{
    public int StatusCode { get; }

    public MarketplaceException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

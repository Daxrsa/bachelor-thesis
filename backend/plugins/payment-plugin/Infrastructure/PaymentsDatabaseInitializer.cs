using Microsoft.EntityFrameworkCore;
using PaymentPlugin.Infrastructure.Persistence;

namespace PaymentPlugin.Infrastructure;

public sealed class PaymentsDatabaseInitializer(PaymentsDbContext db)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """
                    CREATE TABLE IF NOT EXISTS "StripeWebhookEvents" (
                        "EventId" varchar(128) PRIMARY KEY,
                        "EventType" varchar(128) NOT NULL,
                        "ProviderReference" varchar(128) NULL,
                        "CustomerId" varchar(128) NULL,
                        "CustomerEmail" varchar(320) NULL,
                        "Amount" numeric(12,2) NULL,
                        "CurrencyCode" varchar(8) NULL,
                        "Status" varchar(64) NULL,
                        "StripeCreatedAtUtc" timestamp with time zone NOT NULL,
                        "ReceivedAtUtc" timestamp with time zone NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS "IX_StripeWebhookEvents_ReceivedAtUtc"
                        ON "StripeWebhookEvents" ("ReceivedAtUtc");
                    """,
                    cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                await Task.Delay(1000, cancellationToken);
            }
        }

        throw new InvalidOperationException("Payment database did not become ready within 30 seconds", lastError);
    }
}

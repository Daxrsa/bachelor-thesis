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

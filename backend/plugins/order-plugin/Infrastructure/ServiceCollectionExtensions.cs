using Microsoft.EntityFrameworkCore;
using OrderPlugin.Application.Orders;
using OrderPlugin.Infrastructure.Persistence;

namespace OrderPlugin.Infrastructure;

public sealed class OrdersDatabaseInitializer(OrdersDbContext db)
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

        throw new InvalidOperationException("Orders database did not become ready within 30 seconds", lastError);
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<OrdersDatabaseInitializer>();
        return services;
    }
}

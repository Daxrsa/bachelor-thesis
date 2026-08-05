using CartPlugin.Application.Carts;
using CartPlugin.Infrastructure.Messaging;
using CartPlugin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CartPlugin.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCartInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CartsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICartRepository, EfCartRepository>();
        services.AddScoped<CartDatabaseInitializer>();
        return services;
    }

    public static IServiceCollection AddCartMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<ICartEventPublisher, RabbitMqCartEventPublisher>();
        services.AddHostedService<CartEventsConsumer>();
        return services;
    }
}

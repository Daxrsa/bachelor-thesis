using Microsoft.EntityFrameworkCore;
using PaymentPlugin.Application.Payments;
using PaymentPlugin.Infrastructure.Messaging;
using PaymentPlugin.Infrastructure.Persistence;
using PaymentPlugin.Infrastructure.Stripe;

namespace PaymentPlugin.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PaymentsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IProductPriceProjectionRepository, EfProductPriceProjectionRepository>();
        services.AddScoped<PaymentsDatabaseInitializer>();
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        return services;
    }

    public static IServiceCollection AddPaymentMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.Configure<StripeOptions>(configuration.GetSection("Stripe"));
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IPaymentEventPublisher, RabbitMqPaymentEventPublisher>();
        services.AddHostedService<PaymentEventsConsumer>();
        return services;
    }
}

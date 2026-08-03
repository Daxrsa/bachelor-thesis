using CartPlugin.Application.Carts;
using CartPlugin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
}

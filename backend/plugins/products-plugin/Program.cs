using ProductsPlugin.Api;
using ProductsPlugin.Application.Products;
using ProductsPlugin.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing products plugin database connection string");

builder.Services.AddProductsInfrastructure(connectionString);
builder.Services.AddProductsApplication();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<ProductsDatabaseInitializer>();
    await databaseInitializer.InitializeAsync();
}

app.MapProductsEndpoints();

app.Run("http://0.0.0.0:8080");
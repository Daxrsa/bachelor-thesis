using CartPlugin.Api;
using CartPlugin.Application.Carts;
using CartPlugin.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing cart plugin database connection string");

builder.Services.AddCartInfrastructure(connectionString);
builder.Services.AddCartApplication();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<CartDatabaseInitializer>();
    await databaseInitializer.InitializeAsync();
}

app.MapCartEndpoints();

app.Run("http://0.0.0.0:8080");

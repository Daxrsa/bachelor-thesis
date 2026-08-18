using OrderPlugin.Api;
using OrderPlugin.Application.Orders;
using OrderPlugin.Infrastructure;
using OrderPlugin.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing order plugin database connection string");

builder.Services.AddOrderInfrastructure(connectionString);
builder.Services.AddOrderApplication();
builder.Services.AddOrderMessaging(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<OrdersDatabaseInitializer>();
    await databaseInitializer.InitializeAsync();
}

app.MapOrderEndpoints();

app.Run("http://0.0.0.0:8080");

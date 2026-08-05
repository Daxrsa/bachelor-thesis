using PaymentPlugin.Api;
using PaymentPlugin.Application.Payments;
using PaymentPlugin.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing payment plugin database connection string");

builder.Services.AddPaymentInfrastructure(connectionString);
builder.Services.AddPaymentApplication();
builder.Services.AddPaymentMessaging(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<PaymentsDatabaseInitializer>();
    await databaseInitializer.InitializeAsync();
}

app.MapPaymentEndpoints();

app.Run("http://0.0.0.0:8080");

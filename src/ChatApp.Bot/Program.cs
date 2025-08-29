using ChatApp.Bot.Services;
using ChatApp.Core.Configuration;
using ChatApp.Core.Interfaces;
using ChatApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(configure =>
{
    configure.ClearProviders(); // Remove all default providers (including EventLog)
    configure.AddConsole();     // Add only console logging
});

// Configure options from appsettings.json
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<StockApiOptions>(
    builder.Configuration.GetSection("StockApi"));

// Register services
builder.Services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();
builder.Services.AddScoped<IStockService, StockService>();

builder.Services.AddHostedService<StockBotService>();

var host = builder.Build();

try
{
    Console.WriteLine("Starting ChatApp Bot Service...");
    await host.RunAsync();
}
catch (OperationCanceledException)
{
    Console.WriteLine("ChatApp Bot Service was cancelled by user.");
}
catch (Exception ex)
{
    Console.WriteLine($"Application terminated unexpectedly: {ex.Message}");
    throw;
}
finally
{
    Console.WriteLine("ChatApp Bot Service has stopped.");
}
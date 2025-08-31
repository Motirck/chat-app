using ChatApp.Bot.Clients;
using ChatApp.Bot.Interfaces;
using ChatApp.Bot.Services;
using ChatApp.Core.Configuration;
using ChatApp.Core.Interfaces;
using ChatApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddLogging(configure =>
{
    configure.ClearProviders();
    configure.AddConsole();
});

// Configure options from appsettings.json
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<StockApiOptions>(
    builder.Configuration.GetSection("StockApi"));

// Register services
builder.Services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();
builder.Services.AddScoped<IStockApiClient, StockApiClient>();
builder.Services.AddHostedService<StockBotService>();

var host = builder.Build();

try
{
    Console.WriteLine("Starting ChatApp Bot Service...");
    
    // DEBUG: Print final configuration values
    var rabbitConfig = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;
    Console.WriteLine($"Final RabbitMQ Config - Host: '{rabbitConfig.HostName}', Port: {rabbitConfig.Port}, User: '{rabbitConfig.UserName}'");
    
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
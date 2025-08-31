using ChatApp.Core.Dtos;
using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Services;

/// <summary>
/// Background service that handles incoming stock quote responses from RabbitMQ
/// and saves them to database while delegating broadcasting to the Web layer
/// </summary>
public class StockQuoteHandlerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBroker? _messageBroker;
    private readonly ILogger<StockQuoteHandlerService> _logger;

    public StockQuoteHandlerService(
        IServiceProvider serviceProvider,
        IMessageBroker? messageBroker,
        ILogger<StockQuoteHandlerService> logger)
    {
        _serviceProvider = serviceProvider;
        _messageBroker = messageBroker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_messageBroker == null)
        {
            _logger.LogWarning("MessageBroker is not available. Stock quote handling disabled.");
            return;
        }

        try
        {
            _logger.LogInformation("Stock Quote Handler Service started");
            
            // Subscribe to stock quote responses
            await _messageBroker.SubscribeAsync<StockQuoteDto>(HandleStockQuoteResponseAsync);
            
            _logger.LogInformation("Successfully subscribed to stock quote responses");
            
            _messageBroker.StartConsuming();

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stock Quote Handler Service is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Stock Quote Handler Service");
            throw;
        }
    }

    
    private async Task HandleStockQuoteResponseAsync(StockQuoteDto stockQuote)
    {
        _logger.LogInformation("Processing stock quote response for {StockCode} in room {RoomId}", 
            stockQuote.StockCode, stockQuote.RoomId);
        
        try
        {
            if (string.IsNullOrEmpty(stockQuote.Quote))
            {
                _logger.LogWarning("Received empty stock quote for {StockCode} in room {RoomId}", 
                    stockQuote.StockCode, stockQuote.RoomId);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            
            var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepository>();
            var broadcaster = scope.ServiceProvider.GetRequiredService<IStockQuoteBroadcaster>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            var botUser = await userManager.FindByNameAsync("StockBot");
            if (botUser == null)
            {
                _logger.LogError("StockBot user not found in database");
                return;
            }
            
            var chatMessage = new ChatMessage
            {
                Content = stockQuote.Quote,
                Username = "StockBot",
                UserId = botUser.Id,
                RoomId = stockQuote.RoomId, // Set room ID
                Timestamp = DateTime.UtcNow,
                IsStockQuote = true
            };
            
            await chatRepository.AddMessageAsync(chatMessage);
            _logger.LogInformation("Stock quote message saved to database for room {RoomId}", stockQuote.RoomId);
            
            // Broadcast to a specific room
            await broadcaster.BroadcastStockQuoteAsync("StockBot", stockQuote.Quote, chatMessage.Timestamp, stockQuote.RoomId);
            _logger.LogInformation("Stock quote broadcasted to room {RoomId}", stockQuote.RoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stock quote response for {StockCode} in room {RoomId}", 
                stockQuote.StockCode, stockQuote.RoomId);
        }
    }
}
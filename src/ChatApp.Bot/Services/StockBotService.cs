using ChatApp.Core.Dtos;
using ChatApp.Bot.Interfaces;
using ChatApp.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatApp.Bot.Services;

public class StockBotService : BackgroundService
{
    private readonly IMessageBroker _messageBroker;
    private readonly IStockApiClient _stockApiClient;
    private readonly ILogger<StockBotService> _logger;

    public StockBotService(
        IMessageBroker messageBroker,
        IStockApiClient stockApiClient,
        ILogger<StockBotService> logger)
    {
        _messageBroker = messageBroker;
        _stockApiClient = stockApiClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock Bot Service started at: {time}", DateTimeOffset.Now);

        try
        {
            // Subscribe to ALL rooms using wildcard
            await _messageBroker.SubscribeAsync<StockCommandDto>(HandleStockCommandAsync);

            _logger.LogInformation("Stock Bot is listening for stock commands from all rooms...");

            _messageBroker.StartConsuming();

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stock Bot Service is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Stock Bot Service");
            throw;
        }
        finally
        {
            _messageBroker.StopConsuming();
            _logger.LogInformation("Stock Bot Service stopped at: {time}", DateTimeOffset.Now);
        }
    }

    private async Task HandleStockCommandAsync(StockCommandDto command)
    {
        try
        {
            _logger.LogInformation("Processing stock command: {StockCode} for user: {Username} in room: {RoomId}",
                command.StockCode, command.Username, command.RoomId);

            var quoteResult = await _stockApiClient.GetStockQuoteAsync(command.StockCode);

            string quoteMessage;
            if (!string.IsNullOrEmpty(quoteResult))
            {
                quoteMessage = $"🤖 {quoteResult}";
                _logger.LogInformation("Successfully fetched quote for {StockCode} in room {RoomId}",
                    command.StockCode, command.RoomId);
                
                await _messageBroker.PublishStockQuoteAsync(command.StockCode, quoteMessage, command.Username, command.RoomId);
            }
            else
            {
                quoteMessage = $"🤖 Sorry, I couldn't find a quote for {command.StockCode.ToUpper()}. Please check the stock symbol.";
                _logger.LogWarning("No quote found for stock code: {StockCode} in room {RoomId}", 
                    command.StockCode, command.RoomId);

                await _messageBroker.PublishStockQuoteAsync(command.StockCode, quoteMessage, command.Username, command.RoomId);
            }

            _logger.LogInformation("Published stock quote response for {StockCode} to room {RoomId}",
                command.StockCode, command.RoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stock command for {StockCode} by user {Username} in room {RoomId}",
                command.StockCode, command.Username, command.RoomId);

            try
            {
                await _messageBroker.PublishStockQuoteAsync(
                    command.StockCode,
                    $"🤖 Sorry, I encountered an error while fetching the quote for {command.StockCode.ToUpper()}. Please try again later.",
                    command.Username, 
                    command.RoomId);
            }
            catch (Exception publishEx)
            {
                _logger.LogError(publishEx, "Error when sending that the {StockCode} was not found. User {Username} in room {RoomId}",
                    command.StockCode, command.Username, command.RoomId);
                
                await _messageBroker.PublishStockQuoteAsync(
                    command.StockCode,
                    $"🤖 Sorry, I encountered an error while fetching the quote for {command.StockCode.ToUpper()}. Please try again later.",
                    command.Username,
                    command.RoomId); 
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock Bot Service is stopping...");

        try
        {
            _messageBroker.StopConsuming();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while stopping message broker");
        }

        await base.StopAsync(stoppingToken);
    }
}
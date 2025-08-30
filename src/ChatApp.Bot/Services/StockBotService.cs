using ChatApp.Core.Dtos;
using ChatApp.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatApp.Bot.Services;

public class StockBotService : BackgroundService
{
    private readonly IMessageBroker _messageBroker;
    private readonly IStockService _stockService;
    private readonly ILogger<StockBotService> _logger;

    public StockBotService(
        IMessageBroker messageBroker,
        IStockService stockService,
        ILogger<StockBotService> logger)
    {
        _messageBroker = messageBroker;
        _stockService = stockService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock Bot Service started at: {time}", DateTimeOffset.Now);

        try
        {
            // Subscribe to stock command messages using Core DTO
            await _messageBroker.SubscribeAsync<StockCommandDto>(HandleStockCommandAsync);

            _logger.LogInformation("Stock Bot is listening for stock commands...");

            // Start consuming messages
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
            _logger.LogInformation($"DEBUG: Bot received stock command: {command.StockCode} from user: {command.Username}");

            _logger.LogInformation("Processing stock command: {StockCode} for user: {Username}",
                command.StockCode, command.Username);

            // Fetch stock quote using the existing service
            _logger.LogInformation($"DEBUG: Calling StockService for {command.StockCode}...");
            var quoteResult = await _stockService.GetStockQuoteAsync(command.StockCode);
            _logger.LogInformation($"DEBUG: StockService returned: {quoteResult}");

            string quoteMessage;
            if (!string.IsNullOrEmpty(quoteResult))
            {
                quoteMessage = $"🤖 {quoteResult}";
                _logger.LogInformation("Successfully fetched quote for {StockCode}: {Quote}",
                    command.StockCode, quoteResult);
            }
            else
            {
                quoteMessage =
                    $"🤖 Sorry, I couldn't find a quote for {command.StockCode.ToUpper()}. Please check the stock symbol.";
                _logger.LogWarning("No quote found for stock code: {StockCode}", command.StockCode);
            }

            _logger.LogInformation($"DEBUG: Publishing quote response: {quoteMessage}");

            // Publish the response back to the chat using Core DTO
            await _messageBroker.PublishStockQuoteAsync(command.StockCode, quoteMessage, command.Username);

            _logger.LogInformation($"DEBUG: Quote response published successfully!");

            _logger.LogInformation("Published stock quote response for {StockCode} to user {Username}",
                command.StockCode, command.Username);
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"DEBUG: Error in HandleStockCommandAsync: {ex.Message}");

            _logger.LogError(ex, "Error processing stock command for {StockCode} by user {Username}",
                command.StockCode, command.Username);

            // Send error message to chat
            try
            {
                await _messageBroker.PublishStockQuoteAsync(
                    command.StockCode,
                    $"🤖 Sorry, I encountered an error while fetching the quote for {command.StockCode.ToUpper()}. Please try again later.",
                    command.Username);
            }
            catch (Exception publishEx)
            {
                _logger.LogError(publishEx, "Failed to publish error message for stock command");
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
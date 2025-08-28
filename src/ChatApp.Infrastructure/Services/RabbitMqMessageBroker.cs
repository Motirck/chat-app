using System.Text;
using System.Text.Json;
using ChatApp.Core.Configuration;
using ChatApp.Core.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatApp.Infrastructure.Services;

public class RabbitMqMessageBroker : IMessageBroker, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string StockCommandQueue = "stock_commands";
    private const string StockQuoteQueue = "stock_quotes";
    
    /// <summary>
    /// Initializes the RabbitMQ message broker with connection settings and declares required queues.
    /// </summary>
    /// <param name="options">RabbitMQ connection configuration options</param>
    public RabbitMqMessageBroker(IOptions<RabbitMqOptions> options)
    {
        var config = options.Value;
        var factory = new ConnectionFactory() 
        { 
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password
        };
        
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        DeclareQueues();
    }
    
    private void DeclareQueues()
    {
        _channel.QueueDeclareAsync(queue: StockCommandQueue,
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null).GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(queue: StockQuoteQueue,
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Publishes a stock command message to RabbitMQ for the bot service to process.
    /// This is triggered when a user sends a /stock=SYMBOL command in the chat.
    /// </summary>
    /// <param name="stockCode">The stock symbol to fetch quote for (e.g., "aapl.us")</param>
    /// <param name="username">Username of the person who requested the stock quote</param>
    public async Task PublishStockCommandAsync(string stockCode, string username)
    {
        var message = new { StockCode = stockCode, Username = username, Timestamp = DateTime.UtcNow };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await _channel.BasicPublishAsync(exchange: "",
                                        routingKey: StockCommandQueue,
                                        body: body,
                                        mandatory: false);
    }

    /// <summary>
    /// Publishes a stock quote response message to RabbitMQ for the web application to display in chat.
    /// This is used by the bot service to send the formatted stock quote back to the chat room.
    /// </summary>
    /// <param name="stockCode">The stock symbol that was queried</param>
    /// <param name="quote">The formatted quote message (e.g., "AAPL.US quote is $150.00 per share")</param>
    /// <param name="username">Username of the person who originally requested the quote</param>
    public async Task PublishStockQuoteAsync(string stockCode, string quote, string username)
    {
        var message = new { StockCode = stockCode, Quote = quote, Username = username, Timestamp = DateTime.UtcNow };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await _channel.BasicPublishAsync(exchange: "",
                                        routingKey: StockQuoteQueue,
                                        body: body,
                                        mandatory: false);
    }

    /// <summary>
    /// Subscribes to RabbitMQ messages of a specific type and handles them with the provided handler function.
    /// Uses async event consumer to process messages without blocking the thread.
    /// </summary>
    /// <typeparam name="T">The message type to subscribe to (e.g., StockCommand or StockQuote)</typeparam>
    /// <param name="handler">Async function to handle received messages of type T</param>
    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                var deserializedMessage = JsonSerializer.Deserialize<T>(message);
                if (deserializedMessage != null)
                {
                    await handler(deserializedMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        };

        var queueName = typeof(T).Name.Contains("Command") ? StockCommandQueue : StockQuoteQueue;
        _channel.BasicConsumeAsync(queue: queueName,
                                  autoAck: true,
                                  consumer: consumer).GetAwaiter().GetResult();
    }
    
    public void StartConsuming() { }
    
    public void StopConsuming()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
    }
    
    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
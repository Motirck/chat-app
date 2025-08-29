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
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string StockCommandQueue = "stock_commands";
    private const string StockQuoteQueue = "stock_quotes";

    /// <summary>
    /// Initializes the RabbitMQ message broker with connection settings.
    /// Connection is established lazily when first needed.
    /// </summary>
    /// <param name="options">RabbitMQ connection configuration options</param>
    public RabbitMqMessageBroker(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;

        // Debug: Print what configuration we received
        Console.WriteLine(
            $"RabbitMQ Broker Config - Host: {_options.HostName}, Port: {_options.Port}, User: {_options.UserName}");
    }

    /// <summary>
    /// Establishes connection to RabbitMQ and declares required queues
    /// </summary>
    private async Task EnsureConnectionAsync()
    {
        if (_connection != null && _connection.IsOpen && _channel != null)
            return;

        try
        {
            Console.WriteLine($"Connecting to RabbitMQ at {_options.HostName}:{_options.Port}...");

            var factory = new ConnectionFactory()
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            Console.WriteLine("Connected to RabbitMQ successfully!");

            await DeclareQueues();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to RabbitMQ: {ex.Message}");
            throw;
        }
    }

    private async Task DeclareQueues()
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel is not initialized");

        await _channel.QueueDeclareAsync(queue: StockCommandQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await _channel.QueueDeclareAsync(queue: StockQuoteQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        Console.WriteLine("RabbitMQ queues declared successfully!");
    }

    public async Task PublishStockCommandAsync(string stockCode, string username)
    {
        await EnsureConnectionAsync();

        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel is not available");

        var message = new { StockCode = stockCode, Username = username, Timestamp = DateTime.UtcNow };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        Console.WriteLine($"DEBUG: Publishing stock command - StockCode: {stockCode}, Username: {username}");

        await _channel.BasicPublishAsync(exchange: "",
            routingKey: StockCommandQueue,
            body: body,
            mandatory: false);

        Console.WriteLine($"DEBUG: Stock command published to queue: {StockCommandQueue}");
    }

    public async Task PublishStockQuoteAsync(string stockCode, string quote, string username)
    {
        await EnsureConnectionAsync();

        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel is not available");

        var message = new { StockCode = stockCode, Quote = quote, Username = username, Timestamp = DateTime.UtcNow };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        Console.WriteLine(
            $"DEBUG: Publishing stock quote - StockCode: {stockCode}, Quote: {quote}, Username: {username}");

        await _channel.BasicPublishAsync(exchange: "",
            routingKey: StockQuoteQueue,
            body: body,
            mandatory: false);

        Console.WriteLine($"DEBUG: Stock quote published to queue: {StockQuoteQueue}");
    }

    public async Task SubscribeAsync<T>(Func<T, Task> handler) where T : class
    {
        await EnsureConnectionAsync();

        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel is not available");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Console.WriteLine($"DEBUG: Received message from queue: {ea.RoutingKey}");
            Console.WriteLine($"DEBUG: Message content: {message}");

            try
            {
                var deserializedMessage = JsonSerializer.Deserialize<T>(message);
                if (deserializedMessage != null)
                {
                    Console.WriteLine($"DEBUG: Successfully deserialized message of type {typeof(T).Name}");
                    await handler(deserializedMessage);
                    Console.WriteLine($"DEBUG: Handler completed for message type {typeof(T).Name}");
                }
                else
                {
                    Console.WriteLine($"DEBUG: Failed to deserialize message to {typeof(T).Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error processing message: {ex.Message}");
                Console.WriteLine($"DEBUG: Exception details: {ex}");
            }
        };

        var queueName = typeof(T).Name.Contains("Command") ? StockCommandQueue : StockQuoteQueue;
        await _channel.BasicConsumeAsync(queue: queueName,
            autoAck: true,
            consumer: consumer);

        Console.WriteLine($"Subscribed to {queueName} queue for {typeof(T).Name}");
    }

    public void StartConsuming()
    {
        Console.WriteLine("Message consumption started (connection established lazily)");
    }

    public void StopConsuming()
    {
        try
        {
            if (_channel != null)
            {
                _channel.CloseAsync().GetAwaiter().GetResult();
            }

            if (_connection != null)
            {
                _connection.CloseAsync().GetAwaiter().GetResult();
            }

            Console.WriteLine("RabbitMQ connection closed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing RabbitMQ connection: {ex.Message}");
        }
    }

    public void Dispose()
    {
        StopConsuming();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
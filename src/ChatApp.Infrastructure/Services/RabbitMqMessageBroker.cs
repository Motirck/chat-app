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
    private const string ChatExchange = "chat.topic";
    private const string DefaultRoom = "lobby";

    public RabbitMqMessageBroker(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    private async Task EnsureConnectionAsync()
    {
        if (_connection != null && _connection.IsOpen && _channel != null)
            return;

        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await DeclareExchange();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to RabbitMQ: {ex.Message}");
            throw;
        }
    }

    private async Task DeclareExchange()
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel is not initialized");

        // Declare topic exchange
        await _channel.ExchangeDeclareAsync(
            exchange: ChatExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null);

        Console.WriteLine("RabbitMQ topic exchange declared successfully!");
    }

    public async Task PublishStockCommandAsync(string stockCode, string username, string roomId)
    {
        await EnsureConnectionAsync();

        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel is not available");

        var message = new { StockCode = stockCode, Username = username, RoomId = roomId, Timestamp = DateTime.UtcNow };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var routingKey = $"room.{roomId}.commands";

        await _channel.BasicPublishAsync(
            exchange: ChatExchange,
            routingKey: routingKey,
            body: body,
            mandatory: false);

        Console.WriteLine($"Published stock command to room {roomId}");
    }

    public Task PublishStockCommandAsync(string stockCode, string username)
        => PublishStockCommandAsync(stockCode, username, DefaultRoom);

    public async Task PublishStockQuoteAsync(string stockCode, string quote, string username, string roomId)
    {
        await EnsureConnectionAsync();

        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel is not available");

        var message = new
            { StockCode = stockCode, Quote = quote, Username = username, RoomId = roomId, Timestamp = DateTime.UtcNow };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var routingKey = $"room.{roomId}.quotes";

        await _channel.BasicPublishAsync(
            exchange: ChatExchange,
            routingKey: routingKey,
            body: body,
            mandatory: false);

        Console.WriteLine($"Published stock quote to room {roomId}");
    }

    public Task PublishStockQuoteAsync(string stockCode, string quote, string username)
        => PublishStockQuoteAsync(stockCode, quote, username, DefaultRoom);

    public async Task SubscribeAsync<T>(Func<T, Task> handler) where T : class
    {
        // Subscribe to all rooms
        await SubscribeToRoomAsync("*", handler);
    }

    public async Task SubscribeToRoomAsync<T>(string roomId, Func<T, Task> handler) where T : class
    {
        await EnsureConnectionAsync();

        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel is not available");

        var messageType = typeof(T).Name.Contains("Command") ? "commands" : "quotes";
        var routingKey = roomId == "*" ? $"room.*.{messageType}" : $"room.{roomId}.{messageType}";
        var queueName = $"{typeof(T).Name}_{roomId}_{Guid.NewGuid()}";

        // Declare queue
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null);

        // Bind queue to exchange with routing key
        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: ChatExchange,
            routingKey: routingKey);

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

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: true,
            consumer: consumer);

        Console.WriteLine($"Subscribed to {routingKey} for {typeof(T).Name}");
    }

    public void StartConsuming()
    {
        Console.WriteLine("Message consumption started");
    }

    public void StopConsuming()
    {
        try
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
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
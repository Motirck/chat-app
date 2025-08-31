using System.Collections.Concurrent;
using ChatApp.Core.Interfaces;
using ChatApp.Core.Dtos;

namespace ChatApp.Tests.Integration.TestHelpers;

public class FakeMessageBroker : IMessageBroker
{
    public readonly ConcurrentQueue<(string stockCode, string username, string roomId)> PublishedCommands = new();
    public readonly ConcurrentQueue<(string stockCode, string quote, string username, string roomId)> PublishedQuotes = new();

    private const string DefaultRoom = "lobby";

    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly ConcurrentDictionary<(Type type, string roomId), List<Delegate>> _roomHandlers = new();

    public Task PublishStockCommandAsync(string stockCode, string username, string roomId)
    {
        PublishedCommands.Enqueue((stockCode, username, roomId));
        if (_handlers.TryGetValue(typeof(StockCommandDto), out var list))
        {
            foreach (var d in list)
            {
                _ = ((Func<StockCommandDto, Task>)d).Invoke(new StockCommandDto{ StockCode = stockCode, Username = username, RoomId = roomId, Timestamp = DateTime.UtcNow});
            }
        }
        if (_roomHandlers.TryGetValue((typeof(StockCommandDto), roomId), out var roomList))
        {
            foreach (var d in roomList)
            {
                _ = ((Func<StockCommandDto, Task>)d).Invoke(new StockCommandDto{ StockCode = stockCode, Username = username, RoomId = roomId, Timestamp = DateTime.UtcNow});
            }
        }
        return Task.CompletedTask;
    }

    public Task PublishStockCommandAsync(string stockCode, string username)
        => PublishStockCommandAsync(stockCode, username, DefaultRoom);

    public Task PublishStockQuoteAsync(string stockCode, string quote, string username, string roomId)
    {
        PublishedQuotes.Enqueue((stockCode, quote, username, roomId));
        if (_handlers.TryGetValue(typeof(StockQuoteDto), out var list))
        {
            foreach (var d in list)
            {
                _ = ((Func<StockQuoteDto, Task>)d).Invoke(new StockQuoteDto{ StockCode = stockCode, Quote = quote, Username = username, RoomId = roomId, Timestamp = DateTime.UtcNow});
            }
        }
        if (_roomHandlers.TryGetValue((typeof(StockQuoteDto), roomId), out var roomList))
        {
            foreach (var d in roomList)
            {
                _ = ((Func<StockQuoteDto, Task>)d).Invoke(new StockQuoteDto{ StockCode = stockCode, Quote = quote, Username = username, RoomId = roomId, Timestamp = DateTime.UtcNow});
            }
        }
        return Task.CompletedTask;
    }

    public Task PublishStockQuoteAsync(string stockCode, string quote, string username)
        => PublishStockQuoteAsync(stockCode, quote, username, DefaultRoom);

    public Task SubscribeAsync<T>(Func<T, Task> handler) where T : class
    {
        var list = _handlers.GetOrAdd(typeof(T), _ => new List<Delegate>());
        list.Add(handler);
        return Task.CompletedTask;
    }

    public Task SubscribeToRoomAsync<T>(string roomId, Func<T, Task> handler) where T : class
    {
        var key = (typeof(T), roomId);
        var list = _roomHandlers.GetOrAdd(key, _ => new List<Delegate>());
        list.Add(handler);
        return Task.CompletedTask;
    }

    public void StartConsuming() { }
    public void StopConsuming() { }
}

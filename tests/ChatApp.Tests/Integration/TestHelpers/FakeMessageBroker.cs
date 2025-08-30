using System.Collections.Concurrent;
using ChatApp.Core.Interfaces;
using ChatApp.Core.Dtos;

namespace ChatApp.Tests.Integration.TestHelpers;

public class FakeMessageBroker : IMessageBroker
{
    public readonly ConcurrentQueue<(string stockCode, string username)> PublishedCommands = new();
    public readonly ConcurrentQueue<(string stockCode, string quote, string username)> PublishedQuotes = new();

    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public Task PublishStockCommandAsync(string stockCode, string username)
    {
        PublishedCommands.Enqueue((stockCode, username));
        if (_handlers.TryGetValue(typeof(StockCommandDto), out var list))
        {
            foreach (var d in list)
            {
                _ = ((Func<StockCommandDto, Task>)d).Invoke(new StockCommandDto{ StockCode = stockCode, Username = username, Timestamp = DateTime.UtcNow});
            }
        }
        return Task.CompletedTask;
    }

    public Task PublishStockQuoteAsync(string stockCode, string quote, string username)
    {
        PublishedQuotes.Enqueue((stockCode, quote, username));
        if (_handlers.TryGetValue(typeof(StockQuoteDto), out var list))
        {
            foreach (var d in list)
            {
                _ = ((Func<StockQuoteDto, Task>)d).Invoke(new StockQuoteDto{ StockCode = stockCode, Quote = quote, Username = username, Timestamp = DateTime.UtcNow});
            }
        }
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(Func<T, Task> handler) where T : class
    {
        var list = _handlers.GetOrAdd(typeof(T), _ => new List<Delegate>());
        list.Add(handler);
        return Task.CompletedTask;
    }

    public void StartConsuming() { }
    public void StopConsuming() { }
}

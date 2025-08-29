namespace ChatApp.Core.Interfaces;

public interface IMessageBroker
{
    Task PublishStockCommandAsync(string stockCode, string username);
    Task PublishStockQuoteAsync(string stockCode, string quote, string username);
    Task SubscribeAsync<T>(Func<T, Task> handler) where T : class;
    void StartConsuming();
    void StopConsuming();
}
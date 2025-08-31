namespace ChatApp.Core.Interfaces;

public interface IMessageBroker
{
    Task PublishStockCommandAsync(string stockCode, string username, string roomId);
    Task PublishStockQuoteAsync(string stockCode, string quote, string username, string roomId);

    // Backward-compatible overloads defaulting to room "lobby"
    Task PublishStockCommandAsync(string stockCode, string username);
    Task PublishStockQuoteAsync(string stockCode, string quote, string username);

    Task SubscribeAsync<T>(Func<T, Task> handler) where T : class;
    Task SubscribeToRoomAsync<T>(string roomId, Func<T, Task> handler) where T : class;
    void StartConsuming();
    void StopConsuming();
}
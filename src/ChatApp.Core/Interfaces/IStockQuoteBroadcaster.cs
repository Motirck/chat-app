namespace ChatApp.Core.Interfaces;

/// <summary>
/// Interface for broadcasting stock quotes to connected clients
/// </summary>
public interface IStockQuoteBroadcaster
{
    Task BroadcastStockQuoteAsync(string username, string quote, DateTime timestamp, string roomId);
    /// <summary>
    /// Backward-compatible overload that broadcasts to the default room ("lobby").
    /// </summary>
    Task BroadcastStockQuoteAsync(string username, string quote, DateTime timestamp);
}

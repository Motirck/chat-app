namespace ChatApp.Core.Interfaces;

/// <summary>
/// Interface for broadcasting stock quotes to connected clients
/// </summary>
public interface IStockQuoteBroadcaster
{
    Task BroadcastStockQuoteAsync(string username, string quote, DateTime timestamp);
}

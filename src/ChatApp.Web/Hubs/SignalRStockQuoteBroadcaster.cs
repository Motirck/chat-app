using ChatApp.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Web.Hubs;

/// <summary>
/// SignalR implementation of stock quote broadcasting with room support
/// </summary>
public class SignalRStockQuoteBroadcaster : IStockQuoteBroadcaster
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRStockQuoteBroadcaster(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastStockQuoteAsync(string username, string quote, DateTime timestamp, string roomId)
    {
        // Send stock quote only to users in the specific room
        await _hubContext.Clients.Group($"Room_{roomId}")
            .SendAsync("ReceiveStockQuote", username, quote, timestamp, roomId);
    }

    public Task BroadcastStockQuoteAsync(string username, string quote, DateTime timestamp)
        => BroadcastStockQuoteAsync(username, quote, timestamp, "lobby");
}
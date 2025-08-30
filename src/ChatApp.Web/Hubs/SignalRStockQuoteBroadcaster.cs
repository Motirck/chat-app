using ChatApp.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Web.Hubs;

/// <summary>
/// SignalR implementation of stock quote broadcasting
/// </summary>
public class SignalRStockQuoteBroadcaster : IStockQuoteBroadcaster
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRStockQuoteBroadcaster(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastStockQuoteAsync(string username, string quote, DateTime timestamp)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveStockQuote", username, quote, timestamp);
    }
}

using ChatApp.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace ChatApp.Tests.Web;

public class SignalRStockQuoteBroadcasterTests
{
    [Fact(DisplayName = "SignalRStockQuoteBroadcaster broadcasts to all clients"), Trait("Category","Unit"), Trait("Area","Web")]
    public async Task Broadcasts_To_All_Clients()
    {
        var clients = new Mock<IHubClients>();
        var clientProxy = new Mock<IClientProxy>();
        clients.Setup(c => c.All).Returns(clientProxy.Object);
        clientProxy
            .Setup(p => p.SendCoreAsync("ReceiveStockQuote", It.IsAny<object?[]>(), default))
            .Returns(Task.CompletedTask);

        var hubContext = new Mock<IHubContext<ChatHub>>();
        hubContext.Setup(h => h.Clients).Returns(clients.Object);

        var broadcaster = new SignalRStockQuoteBroadcaster(hubContext.Object);
        var when = DateTime.UtcNow;

        await broadcaster.BroadcastStockQuoteAsync("StockBot", "AAPL quote is $1.00 per share", when);

        clientProxy.Verify(
            p => p.SendCoreAsync(
                "ReceiveStockQuote",
                It.Is<object?[]>(args =>
                    args.Length == 3 &&
                    args[0] != null && args[0] is string && (string)args[0] == "StockBot" &&
                    args[1] != null && args[1] is string && ((string)args[1]).Contains("AAPL") &&
                    args[2] != null && args[2] is DateTime && (DateTime)args[2] == when),
                default),
            Times.Once);
    }
}

using ChatApp.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace ChatApp.Tests.Web;

public class SignalRStockQuoteBroadcasterTests
{
    [Fact(DisplayName = "SignalRStockQuoteBroadcaster broadcasts to room group"), Trait("Category","Unit"), Trait("Area","Web")]
    public async Task Broadcasts_To_Room_Group()
    {
        var clients = new Mock<IHubClients>();
        var groupManager = new Mock<IGroupManager>();
        var clientProxy = new Mock<IClientProxy>();

        clients.Setup(c => c.Group("Room_lobby")).Returns(clientProxy.Object);
        clientProxy
            .Setup(p => p.SendCoreAsync("ReceiveStockQuote", It.IsAny<object?[]>(), default))
            .Returns(Task.CompletedTask);

        var hubContext = new Mock<IHubContext<ChatHub>>();
        hubContext.Setup(h => h.Clients).Returns(clients.Object);

        var broadcaster = new SignalRStockQuoteBroadcaster(hubContext.Object);
        var when = DateTime.UtcNow;

        await broadcaster.BroadcastStockQuoteAsync("StockBot", "AAPL quote is $1.00 per share", when, "lobby");

        clientProxy.Verify(
            p => p.SendCoreAsync(
                "ReceiveStockQuote",
                It.Is<object?[]>(args =>
                    args.Length == 4 &&
                    args[0] != null && args[0] is string && (string)args[0] == "StockBot" &&
                    args[1] != null && args[1] is string && ((string)args[1]).Contains("AAPL") &&
                    args[2] != null && args[2] is DateTime && (DateTime)args[2] == when &&
                    args[3] != null && args[3] is string && (string)args[3] == "lobby"),
                default),
            Times.Once);
    }
}

using ChatApp.Bot.Services;
using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces;
using ChatApp.Infrastructure.Services;
using ChatApp.Tests.Integration.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChatApp.Tests.Integration;

public class StockCommandFlowTests
{
    private static UserManager<ApplicationUser> CreateUserManagerMock(ApplicationUser? stockBot)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        mgr.Setup(m => m.FindByNameAsync("StockBot")).ReturnsAsync(stockBot!);
        return mgr.Object;
    }

    private static IServiceProvider CreateScopedProvider(IChatRepository repo, IStockQuoteBroadcaster broadcaster, UserManager<ApplicationUser> um)
    {
        var scope = new Mock<IServiceScope>();
        var sp = new Mock<IServiceProvider>();
        sp.Setup(s => s.GetService(typeof(IChatRepository))).Returns(repo);
        sp.Setup(s => s.GetService(typeof(IStockQuoteBroadcaster))).Returns(broadcaster);
        sp.Setup(s => s.GetService(typeof(UserManager<ApplicationUser>))).Returns(um);
        scope.SetupGet(s => s.ServiceProvider).Returns(sp.Object);
        var sf = new Mock<IServiceScopeFactory>();
        sf.Setup(f => f.CreateScope()).Returns(scope.Object);
        var root = new ServiceCollection().AddSingleton(sf.Object).BuildServiceProvider();
        return new SP(root, sf.Object);
    }

    private sealed class SP : IServiceProvider
    {
        private readonly IServiceProvider _root; private readonly IServiceScopeFactory _sf;
        public SP(IServiceProvider root, IServiceScopeFactory sf) { _root = root; _sf = sf; }
        public object? GetService(Type serviceType) => serviceType == typeof(IServiceScopeFactory) ? _sf : _root.GetService(serviceType);
    }

    [Fact(DisplayName = "Flow: /stock command goes through bot -> quote handler -> repository/broadcaster"), Trait("Category","Integration"), Trait("Area","Flow")]
    public async Task Full_Stock_Command_Flow()
    {
        // Arrange: fake broker and services
        var broker = new FakeMessageBroker();
        var stockService = new Mock<IStockService>();
        stockService.Setup(s => s.GetStockQuoteAsync("aapl")).ReturnsAsync("AAPL quote is $123.45 per share");

        var repo = new Mock<IChatRepository>();
        ChatApp.Core.Entities.ChatMessage? savedMsg = null;
        repo.Setup(r => r.AddMessageAsync(It.IsAny<ChatApp.Core.Entities.ChatMessage>())).ReturnsAsync((ChatApp.Core.Entities.ChatMessage m) => { savedMsg = m; return m; });
        var broadcaster = new Mock<IStockQuoteBroadcaster>();
        var userManager = CreateUserManagerMock(new ApplicationUser { Id = "bot-id", UserName = "StockBot" });
        var sp = CreateScopedProvider(repo.Object, broadcaster.Object, userManager);

        var bot = new StockBotService(broker, stockService.Object, NullLogger<StockBotService>.Instance);
        var handler = new StockQuoteHandlerService(sp, broker, NullLogger<StockQuoteHandlerService>.Instance);

        var cts = new CancellationTokenSource();
        var botRun = bot.StartAsync(cts.Token);
        var handlerRun = handler.StartAsync(cts.Token);

        // Act: simulate hub sending /stock=aapl via broker.PublishStockCommandAsync
        await broker.PublishStockCommandAsync("aapl", "john");

        // Allow the flow to process
        await Task.Delay(50);

        // Cleanup
        cts.Cancel();
        try { await Task.WhenAll(botRun, handlerRun); } catch { }

        // Assert: quote published and persisted/broadcasted by handler
        broker.PublishedQuotes.Should().NotBeEmpty();
        broker.PublishedQuotes.Any(q => q.stockCode == "aapl" && q.username == "john" && q.quote.Contains("AAPL quote is")).Should().BeTrue();
        savedMsg.Should().NotBeNull();
        savedMsg!.IsStockQuote.Should().BeTrue();
        savedMsg.Username.Should().Be("StockBot");
        broadcaster.Verify(b => b.BroadcastStockQuoteAsync("StockBot", It.Is<string>(s => s.Contains("AAPL quote")), It.IsAny<DateTime>()), Times.AtLeastOnce);
    }
}

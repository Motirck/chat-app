using ChatApp.Core.Dtos;
using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces;
using ChatApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChatApp.Tests.Infrastructure;

public class StockQuoteHandlerServiceTests
{
    private static UserManager<ApplicationUser> CreateUserManagerMock(out Mock<IUserStore<ApplicationUser>> storeMock, ApplicationUser? findResult)
    {
        storeMock = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(storeMock.Object, null, null, null, null, null, null, null, null);
        mgr.Setup(m => m.FindByNameAsync("StockBot")).ReturnsAsync(findResult!);
        return mgr.Object;
    }

    private static IServiceProvider CreateScopedProvider(Mock<IChatRepository> repoMock, Mock<IStockQuoteBroadcaster> broadcasterMock, UserManager<ApplicationUser> userManager)
    {
        var scopeMock = new Mock<IServiceScope>();
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(s => s.GetService(typeof(IChatRepository))).Returns(repoMock.Object);
        spMock.Setup(s => s.GetService(typeof(IStockQuoteBroadcaster))).Returns(broadcasterMock.Object);
        spMock.Setup(s => s.GetService(typeof(UserManager<ApplicationUser>))).Returns(userManager);
        scopeMock.SetupGet(s => s.ServiceProvider).Returns(spMock.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var root = new ServiceCollection()
            .AddSingleton(scopeFactory.Object)
            .BuildServiceProvider();

        return new ScopedServiceProvider(root, scopeFactory.Object);
    }

    private sealed class ScopedServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _root;
        private readonly IServiceScopeFactory _factory;
        public ScopedServiceProvider(IServiceProvider root, IServiceScopeFactory factory)
        {
            _root = root; _factory = factory;
        }
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory)) return _factory;
            return _root.GetService(serviceType);
        }
    }

    [Fact(DisplayName = "StockQuoteHandlerService: saves and broadcasts"), Trait("Category","Unit"), Trait("Area","Infrastructure")]
    public async Task Saves_And_Broadcasts()
    {
        var repo = new Mock<IChatRepository>();
        var broadcaster = new Mock<IStockQuoteBroadcaster>();
        var botUser = new ApplicationUser { Id = "bot-id", UserName = "StockBot" };
        var userManager = CreateUserManagerMock(out _, botUser);
        var sp = CreateScopedProvider(repo, broadcaster, userManager);

        var broker = new Mock<IMessageBroker>();
        Func<StockQuoteDto, Task>? captured = null;
        broker.Setup(b => b.SubscribeAsync<StockQuoteDto>(It.IsAny<Func<StockQuoteDto, Task>>()))
            .Callback<Func<StockQuoteDto, Task>>(h => captured = h)
            .Returns(Task.CompletedTask);
        broker.Setup(b => b.StartConsuming());

        var service = new StockQuoteHandlerService(sp, broker.Object, NullLogger<StockQuoteHandlerService>.Instance);
        var cts = new CancellationTokenSource();
        var runTask = service.StartAsync(cts.Token);
        await Task.Delay(10);
        captured.Should().NotBeNull();

        repo.Setup(r => r.AddMessageAsync(It.IsAny<ChatMessage>())).ReturnsAsync((ChatMessage m) => m);
        broadcaster.Setup(b => b.BroadcastStockQuoteAsync("StockBot", It.Is<string>(q => q.Contains("price")), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        await captured!(new StockQuoteDto { StockCode = "AAPL", Quote = "price is 1", Username = "john", Timestamp = DateTime.UtcNow });

        repo.Verify(r => r.AddMessageAsync(It.Is<ChatMessage>(m => m.UserId == "bot-id" && m.IsStockQuote)), Times.Once);
        broadcaster.VerifyAll();

        cts.Cancel();
        try { await runTask; } catch { }
    }

    [Fact(DisplayName = "StockQuoteHandlerService: empty quote skips save/broadcast"), Trait("Category","Unit"), Trait("Area","Infrastructure")]
    public async Task Empty_Quote_Skips()
    {
        var repo = new Mock<IChatRepository>(MockBehavior.Strict);
        var broadcaster = new Mock<IStockQuoteBroadcaster>(MockBehavior.Strict);
        var userManager = CreateUserManagerMock(out _, new ApplicationUser { Id = "bot-id", UserName = "StockBot" });
        var sp = CreateScopedProvider(repo, broadcaster, userManager);

        var broker = new Mock<IMessageBroker>();
        Func<StockQuoteDto, Task>? captured = null;
        broker.Setup(b => b.SubscribeAsync<StockQuoteDto>(It.IsAny<Func<StockQuoteDto, Task>>()))
            .Callback<Func<StockQuoteDto, Task>>(h => captured = h)
            .Returns(Task.CompletedTask);
        broker.Setup(b => b.StartConsuming());

        var service = new StockQuoteHandlerService(sp, broker.Object, NullLogger<StockQuoteHandlerService>.Instance);
        var cts = new CancellationTokenSource();
        var runTask = service.StartAsync(cts.Token);
        await Task.Delay(10);
        captured.Should().NotBeNull();

        await captured!(new StockQuoteDto { StockCode = "AAPL", Quote = "", Username = "john", Timestamp = DateTime.UtcNow });

        repo.VerifyNoOtherCalls();
        broadcaster.VerifyNoOtherCalls();

        cts.Cancel();
        try { await runTask; } catch { }
    }
}

using ChatApp.Bot.Services;
using ChatApp.Core.Dtos;
using ChatApp.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace ChatApp.Tests.Bot;

public class StockBotServiceTests : IClassFixture<StockBotFixture>
{
    private readonly StockBotFixture _fx;
    public StockBotServiceTests(StockBotFixture fx) => _fx = fx;
    private StockBotService CreateService() => _fx.CreateService();

    [Fact(DisplayName = "ExecuteAsync subscribes and starts consuming"), Trait("Category", "Unit"), Trait("Area", "Bot")]
    public async Task ExecuteAsync_Should_Subscribe_And_StartConsuming()
    {
        // Arrange
        var brokerMock = _fx.BrokerMock; brokerMock.Reset();
        var stockMock = _fx.StockMock; stockMock.Reset();
        var tcs = new CancellationTokenSource();
        var registeredHandler = default(Func<StockCommandDto, Task>);

        brokerMock
            .Setup(b => b.SubscribeAsync<StockCommandDto>(It.IsAny<Func<StockCommandDto, Task>>()))
            .Callback<Func<StockCommandDto, Task>>(h => registeredHandler = h)
            .Returns(Task.CompletedTask);

        brokerMock.Setup(b => b.StartConsuming());
        brokerMock.Setup(b => b.StopConsuming());

        var service = CreateService();

        // Act - run the background service briefly and then cancel
        var runTask = service.StartAsync(tcs.Token);
        await Task.Delay(50);
        tcs.Cancel();
        try { await runTask; } catch { /* BackgroundService may surface cancellation */ }

        // Assert
        brokerMock.Verify(b => b.SubscribeAsync<StockCommandDto>(It.IsAny<Func<StockCommandDto, Task>>()), Times.Once);
        brokerMock.Verify(b => b.StartConsuming(), Times.Once);
        brokerMock.Verify(b => b.StopConsuming(), Times.AtLeastOnce);
    }

    [Fact(DisplayName = "HandleStockCommandAsync publishes success quote"), Trait("Category", "Unit"), Trait("Area", "Bot")]
    public async Task HandleStockCommandAsync_Publishes_Success_Message()
    {
        // Arrange
        var brokerMock = _fx.BrokerMock; brokerMock.Reset();
        var stockMock = _fx.StockMock; stockMock.Reset();

        brokerMock
            .Setup(b => b.SubscribeAsync<StockCommandDto>(It.IsAny<Func<StockCommandDto, Task>>()))
            .Returns(Task.CompletedTask);
        brokerMock.Setup(b => b.StartConsuming());
        brokerMock.Setup(b => b.StopConsuming());

        stockMock.Setup(s => s.GetStockQuoteAsync("aapl")).ReturnsAsync("AAPL quote is 100");
        brokerMock
            .Setup(b => b.PublishStockQuoteAsync("aapl", It.Is<string>(m => m.Contains("AAPL quote is 100")), "john"))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Capture the handler passed into SubscribeAsync and invoke it
        Func<StockCommandDto, Task>? capturedHandler = null;
        brokerMock.Reset();
        brokerMock
            .Setup(b => b.SubscribeAsync<StockCommandDto>(It.IsAny<Func<StockCommandDto, Task>>()))
            .Callback<Func<StockCommandDto, Task>>(h => capturedHandler = h)
            .Returns(Task.CompletedTask);
        brokerMock.Setup(b => b.StartConsuming());
        brokerMock.Setup(b => b.StopConsuming());
        stockMock.Setup(s => s.GetStockQuoteAsync("aapl")).ReturnsAsync("AAPL quote is 100");
        brokerMock
            .Setup(b => b.PublishStockQuoteAsync("aapl", It.Is<string>(m => m.Contains("AAPL quote is 100")), "john"))
            .Returns(Task.CompletedTask);

        var cts = new CancellationTokenSource();
        var run = service.StartAsync(cts.Token);
        await Task.Delay(10);
        capturedHandler.Should().NotBeNull();

        // Act
        await capturedHandler!(new StockCommandDto { StockCode = "aapl", Username = "john" });

        // Cleanup
        cts.Cancel();
        try { await run; } catch { }

        // Assert
        stockMock.Verify(s => s.GetStockQuoteAsync("aapl"), Times.Once);
        brokerMock.Verify(b => b.PublishStockQuoteAsync("aapl", It.IsAny<string>(), "john"), Times.Once);
    }

    [Fact(DisplayName = "HandleStockCommandAsync publishes error on exception"), Trait("Category", "Unit"), Trait("Area", "Bot")]
    public async Task HandleStockCommandAsync_Publishes_Error_On_Exception()
    {
        // Arrange
        var brokerMock = _fx.BrokerMock; brokerMock.Reset();
        var stockMock = _fx.StockMock; stockMock.Reset();

        Func<StockCommandDto, Task>? capturedHandler = null;

        brokerMock
            .Setup(b => b.SubscribeAsync<StockCommandDto>(It.IsAny<Func<StockCommandDto, Task>>()))
            .Callback<Func<StockCommandDto, Task>>(h => capturedHandler = h)
            .Returns(Task.CompletedTask);
        brokerMock.Setup(b => b.StartConsuming());
        brokerMock.Setup(b => b.StopConsuming());

        stockMock.Setup(s => s.GetStockQuoteAsync("msft")).ThrowsAsync(new Exception("boom"));
        brokerMock
            .Setup(b => b.PublishStockQuoteAsync("msft", It.Is<string>(m => m.Contains("error") || m.Contains("Sorry")), "alice"))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var cts = new CancellationTokenSource();
        var run = service.StartAsync(cts.Token);
        await Task.Delay(10);
        capturedHandler.Should().NotBeNull();

        // Act
        await capturedHandler!(new StockCommandDto { StockCode = "msft", Username = "alice" });

        // Cleanup
        cts.Cancel();
        try { await run; } catch { }

        // Assert
        stockMock.Verify(s => s.GetStockQuoteAsync("msft"), Times.Once);
        brokerMock.Verify(b => b.PublishStockQuoteAsync("msft", It.IsAny<string>(), "alice"), Times.Once);
    }

    [Fact(DisplayName = "StockBot: empty quote publishes friendly message"), Trait("Category","Unit"), Trait("Area","Bot")]
    public async Task Empty_Quote_Publishes_Friendly_Message()
    {
        var broker = _fx.BrokerMock; broker.Reset();
        var stock = _fx.StockMock; stock.Reset();

        Func<StockCommandDto, Task>? captured = null;
        broker.Setup(b => b.SubscribeAsync<StockCommandDto>(It.IsAny<Func<StockCommandDto, Task>>()))
              .Callback<Func<StockCommandDto, Task>>(h => captured = h)
              .Returns(Task.CompletedTask);
        broker.Setup(b => b.StartConsuming());
        broker.Setup(b => b.StopConsuming());

        stock.Setup(s => s.GetStockQuoteAsync("tsla")).ReturnsAsync((string?)null);
        broker.Setup(b => b.PublishStockQuoteAsync("tsla", It.Is<string>(m => m.Contains("couldn't find a quote") || m.Contains("Sorry")), "john"))
              .Returns(Task.CompletedTask);

        var svc = CreateService();
        var cts = new CancellationTokenSource();
        var run = svc.StartAsync(cts.Token);
        await Task.Delay(10);
        captured!.Invoke(new StockCommandDto { StockCode = "tsla", Username = "john" }).Wait();
        cts.Cancel();
        try { await run; } catch { }

        stock.Verify(s => s.GetStockQuoteAsync("tsla"), Times.Once);
        broker.Verify(b => b.PublishStockQuoteAsync("tsla", It.IsAny<string>(), "john"), Times.Once);
    }

    [Fact(DisplayName = "StockBot: StopAsync stops broker"), Trait("Category","Unit"), Trait("Area","Bot")]
    public async Task StopAsync_Stops_Broker()
    {
        var broker = _fx.BrokerMock; broker.Reset();
        var stock = _fx.StockMock; stock.Reset();
        broker.Setup(b => b.StopConsuming());

        var svc = CreateService();
        await svc.StopAsync(CancellationToken.None);

        broker.Verify(b => b.StopConsuming(), Times.Once);
    }
}

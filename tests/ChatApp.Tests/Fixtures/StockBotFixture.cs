using ChatApp.Bot.Services;
using ChatApp.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChatApp.Tests.Fixtures;

public class StockBotFixture
{
    public Mock<IMessageBroker> BrokerMock { get; } = new(MockBehavior.Strict);
    public Mock<IStockService> StockMock { get; } = new(MockBehavior.Strict);
    public ILogger<StockBotService> Logger { get; } = NullLogger<StockBotService>.Instance;

    public StockBotService CreateService() => new(BrokerMock.Object, StockMock.Object, Logger);
}

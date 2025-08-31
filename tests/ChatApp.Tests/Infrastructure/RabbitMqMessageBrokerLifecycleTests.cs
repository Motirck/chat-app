using ChatApp.Core.Configuration;
using ChatApp.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace ChatApp.Tests.Infrastructure;

public class RabbitMqMessageBrokerLifecycleTests
{
    private static IOptions<RabbitMqOptions> MakeOptions() => Options.Create(new RabbitMqOptions
    {
        HostName = "invalid-host-local-test", // ensure no accidental connection
        Port = 5672,
        UserName = "guest",
        Password = "guest"
    });

    [Fact(DisplayName = "Broker lifecycle: Start/Stop without connection are safe"), Trait("Category","Unit"), Trait("Area","Infrastructure")]
    public void Start_Stop_Should_Not_Throw()
    {
        var broker = new RabbitMqMessageBroker(MakeOptions());
        broker.StartConsuming();
        broker.StopConsuming();
        // No assertions: test passes if no exception is thrown
    }

    [Fact(DisplayName = "Broker lifecycle: Dispose is idempotent"), Trait("Category","Unit"), Trait("Area","Infrastructure")]
    public void Dispose_Should_Not_Throw_When_Called_Twice()
    {
        var broker = new RabbitMqMessageBroker(MakeOptions());
        broker.Dispose();
        broker.Dispose();
    }
}

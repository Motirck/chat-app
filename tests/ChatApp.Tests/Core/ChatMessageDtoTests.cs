using ChatApp.Core.Dtos;
using FluentAssertions;

namespace ChatApp.Tests.Core;

public class ChatMessageDtoTests
{
    [Fact(DisplayName = "ChatMessageDto: default values are sane"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Default_Values_Should_Be_Sane()
    {
        var dto = new ChatMessageDto();
        dto.Id.Should().Be(0);
        dto.Content.Should().BeEmpty();
        dto.Username.Should().BeEmpty();
        dto.Timestamp.Should().Be(default);
        dto.IsStockQuote.Should().BeFalse();
    }

    [Fact(DisplayName = "ChatMessageDto: property initialization works"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Property_Initialization_Works()
    {
        var when = DateTime.UtcNow;
        var dto = new ChatMessageDto
        {
            Id = 42,
            Content = "Hello World",
            Username = "john",
            Timestamp = when,
            IsStockQuote = true
        };

        dto.Id.Should().Be(42);
        dto.Content.Should().Be("Hello World");
        dto.Username.Should().Be("john");
        dto.Timestamp.Should().BeCloseTo(when, TimeSpan.FromSeconds(1));
        dto.IsStockQuote.Should().BeTrue();
    }
}

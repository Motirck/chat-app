using ChatApp.Core.Entities;
using ChatApp.Core.Entities.Validators;
using FluentAssertions;

namespace ChatApp.Tests.Core;

public class ChatMessageValidatorTests
{
    private readonly ChatMessageValidator _validator = new();

    [Fact(DisplayName = "Validator: valid chat message passes"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Valid_Message_Should_Pass()
    {
        var msg = new ChatMessage { Content = "Hello", Timestamp = DateTime.UtcNow, UserId = "u1", Username = "john" };
        var result = _validator.Validate(msg);
        result.IsValid.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact(DisplayName = "Validator: invalid chat message fails"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Invalid_Message_Should_Fail()
    {
        var msg = new ChatMessage { Content = "", Timestamp = default, UserId = "", Username = "" };
        var result = _validator.Validate(msg);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChatMessage.Content));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChatMessage.UserId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChatMessage.Username));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChatMessage.Timestamp));
    }
}

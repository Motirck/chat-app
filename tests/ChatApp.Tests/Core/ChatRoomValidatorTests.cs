using ChatApp.Core.Entities;
using ChatApp.Core.Entities.Validators;

namespace ChatApp.Tests.Core;

public class ChatRoomValidatorTests
{
    [Fact]
    public void Valid_ChatRoom_Passes()
    {
        var room = new ChatRoom
        {
            Id = "lobby",
            Name = "Lobby",
            Description = "General chat",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var validator = new ChatRoomValidator();
        var result = validator.Validate(room);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("too#bad")]
    public void Invalid_Id_Fails(string id)
    {
        var room = new ChatRoom
        {
            Id = id,
            Name = "Lobby",
            CreatedAt = DateTime.UtcNow
        };

        var validator = new ChatRoomValidator();
        var result = validator.Validate(room);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var room = new ChatRoom
        {
            Id = "tech",
            Name = "",
            CreatedAt = DateTime.UtcNow
        };

        var validator = new ChatRoomValidator();
        var result = validator.Validate(room);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Default_CreatedAt_Fails()
    {
        var room = new ChatRoom
        {
            Id = "tech",
            Name = "Tech",
            CreatedAt = default
        };

        var validator = new ChatRoomValidator();
        var result = validator.Validate(room);

        Assert.False(result.IsValid);
    }
}

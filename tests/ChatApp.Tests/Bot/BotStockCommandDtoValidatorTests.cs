using ChatApp.Bot.Dtos;
using ChatApp.Bot.Dtos.Validators;
using FluentAssertions;

namespace ChatApp.Tests.Bot;

public class BotStockCommandDtoValidatorTests
{
    private readonly BotStockCommandDtoValidator _validator = new();

    [Fact(DisplayName = "Bot DTO Validator: valid command passes"), Trait("Category","Unit"), Trait("Area","Bot")]
    public void Valid_Command_Should_Pass()
    {
        var dto = new BotStockCommandDto { StockCode = "AAPL", Username = "john", RoomId = "lobby" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact(DisplayName = "Bot DTO Validator: empty fields fail"), Trait("Category","Unit"), Trait("Area","Bot")]
    public void Empty_Fields_Should_Fail()
    {
        var dto = new BotStockCommandDto { StockCode = "", Username = "", RoomId = "" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BotStockCommandDto.StockCode));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BotStockCommandDto.Username));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BotStockCommandDto.RoomId));
    }

    [Fact(DisplayName = "Bot DTO Validator: invalid stock code format fails"), Trait("Category","Unit"), Trait("Area","Bot")]
    public void Invalid_StockCode_Should_Fail()
    {
        var dto = new BotStockCommandDto { StockCode = "AAPL-1", Username = "john", RoomId = "lobby" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BotStockCommandDto.StockCode));
    }
}
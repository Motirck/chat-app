using ChatApp.Core.Dtos;
using ChatApp.Core.Dtos.Validators;
using FluentAssertions;

namespace ChatApp.Tests.Core;

public class StockCommandDtoValidatorTests
{
    private readonly StockCommandDtoValidator _validator = new();

    [Fact(DisplayName = "Validator: valid command passes"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Valid_Command_Should_Pass()
    {
        var dto = new StockCommandDto { StockCode = "AAPL", Username = "john" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact(DisplayName = "Validator: empty fields fail"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Empty_Fields_Should_Fail()
    {
        var dto = new StockCommandDto { StockCode = "", Username = "" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockCommandDto.StockCode));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockCommandDto.Username));
    }

    [Fact(DisplayName = "Validator: invalid stock code format fails"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Invalid_StockCode_Should_Fail()
    {
        var dto = new StockCommandDto { StockCode = "AAPL-1", Username = "john" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockCommandDto.StockCode));
    }
}

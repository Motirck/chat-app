using ChatApp.Core.Dtos;
using ChatApp.Core.Dtos.Validators;
using FluentAssertions;

namespace ChatApp.Tests.Core;

public class StockQuoteDtoValidatorTests
{
    private readonly StockQuoteDtoValidator _validator = new();

    [Fact(DisplayName = "Validator: valid stock quote passes"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Valid_Quote_Should_Pass()
    {
        var dto = new StockQuoteDto { StockCode = "MSFT", Username = "alice", Quote = "MSFT quote is 123", Timestamp = DateTime.UtcNow };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact(DisplayName = "Validator: empty fields fail"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Empty_Fields_Should_Fail()
    {
        var dto = new StockQuoteDto { StockCode = "", Username = "", Quote = "", Timestamp = default };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockQuoteDto.StockCode));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockQuoteDto.Username));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockQuoteDto.Quote));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockQuoteDto.Timestamp));
    }

    [Fact(DisplayName = "Validator: invalid stock code format fails"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Invalid_StockCode_Should_Fail()
    {
        var dto = new StockQuoteDto { StockCode = "MSFT-1", Username = "alice", Quote = "q", Timestamp = DateTime.UtcNow };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockQuoteDto.StockCode));
    }
}

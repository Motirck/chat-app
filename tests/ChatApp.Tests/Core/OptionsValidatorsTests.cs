using ChatApp.Core.Configuration;
using ChatApp.Core.Configuration.Validators;
using FluentAssertions;

namespace ChatApp.Tests.Core;

public class OptionsValidatorsTests
{
    [Fact(DisplayName = "Validator: StockApiOptions valid config passes"), Trait("Category","Unit"), Trait("Area","Core")]
    public void StockApiOptions_Valid_Should_Pass()
    {
        var options = new StockApiOptions { BaseUrl = "https://api.example.com", Format = "csv", Export = "file" };
        var validator = new StockApiOptionsValidator();
        var result = validator.Validate(options);
        result.IsValid.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact(DisplayName = "Validator: StockApiOptions invalid config fails"), Trait("Category","Unit"), Trait("Area","Core")]
    public void StockApiOptions_Invalid_Should_Fail()
    {
        var options = new StockApiOptions { BaseUrl = "not-a-url", Format = "", Export = "" };
        var validator = new StockApiOptionsValidator();
        var result = validator.Validate(options);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockApiOptions.BaseUrl));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockApiOptions.Format));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StockApiOptions.Export));
    }

    [Fact(DisplayName = "Validator: RabbitMqOptions valid config passes"), Trait("Category","Unit"), Trait("Area","Core")]
    public void RabbitMqOptions_Valid_Should_Pass()
    {
        var options = new RabbitMqOptions { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
        var validator = new RabbitMqOptionsValidator();
        var result = validator.Validate(options);
        result.IsValid.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact(DisplayName = "Validator: RabbitMqOptions invalid config fails"), Trait("Category","Unit"), Trait("Area","Core")]
    public void RabbitMqOptions_Invalid_Should_Fail()
    {
        var options = new RabbitMqOptions { HostName = "", Port = 0, UserName = "", Password = "" };
        var validator = new RabbitMqOptionsValidator();
        var result = validator.Validate(options);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RabbitMqOptions.HostName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RabbitMqOptions.Port));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RabbitMqOptions.UserName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RabbitMqOptions.Password));
    }
}

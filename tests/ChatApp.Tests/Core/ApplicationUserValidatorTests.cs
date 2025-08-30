using ChatApp.Core.Entities;
using ChatApp.Core.Entities.Validators;
using FluentAssertions;

namespace ChatApp.Tests.Core;

public class ApplicationUserValidatorTests
{
    private readonly ApplicationUserValidator _validator = new();

    [Fact(DisplayName = "Validator: valid application user passes"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Valid_User_Should_Pass()
    {
        var user = new ApplicationUser { UserName = "john", Email = "john@example.com", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow };
        var result = _validator.Validate(user);
        result.IsValid.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact(DisplayName = "Validator: invalid application user fails"), Trait("Category","Unit"), Trait("Area","Core")]
    public void Invalid_User_Should_Fail()
    {
        var user = new ApplicationUser { UserName = "", Email = "not-an-email", CreatedAt = default };
        var result = _validator.Validate(user);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ApplicationUser.UserName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ApplicationUser.Email));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ApplicationUser.CreatedAt));
    }
}

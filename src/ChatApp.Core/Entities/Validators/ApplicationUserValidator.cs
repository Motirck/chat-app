using FluentValidation;

namespace ChatApp.Core.Entities.Validators;

public class ApplicationUserValidator : AbstractValidator<ApplicationUser>
{
    public ApplicationUserValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty();

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.CreatedAt)
            .NotEqual(default(DateTime));

        RuleFor(x => x.LastLoginAt)
            .NotEqual(default(DateTime)).When(x => x.LastLoginAt != default);
    }
}

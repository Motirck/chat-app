using FluentValidation;

namespace ChatApp.Core.Configuration.Validators;

public class RabbitMqOptionsValidator : AbstractValidator<RabbitMqOptions>
{
    public RabbitMqOptionsValidator()
    {
        RuleFor(x => x.HostName)
            .NotEmpty();

        RuleFor(x => x.Port)
            .GreaterThan(0);

        RuleFor(x => x.UserName)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}

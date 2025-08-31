using FluentValidation;

namespace ChatApp.Core.Configuration.Validators;

public class StockApiOptionsValidator : AbstractValidator<StockApiOptions>
{
    public StockApiOptionsValidator()
    {
        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("BaseUrl must be a valid absolute URL");

        RuleFor(x => x.Format)
            .NotEmpty();

        RuleFor(x => x.Export)
            .NotEmpty();
    }
}

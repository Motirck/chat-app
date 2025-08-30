using FluentValidation;

namespace ChatApp.Core.Dtos.Validators;

public class StockQuoteDtoValidator : AbstractValidator<StockQuoteDto>
{
    public StockQuoteDtoValidator()
    {
        RuleFor(x => x.StockCode)
            .NotEmpty()
            .MaximumLength(10)
            .Matches("^[a-zA-Z.]+$");

        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Quote)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Timestamp)
            .NotEqual(default(DateTime));
    }
}

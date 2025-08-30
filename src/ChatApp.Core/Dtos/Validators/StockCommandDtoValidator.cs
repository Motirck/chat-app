using FluentValidation;

namespace ChatApp.Core.Dtos.Validators;

public class StockCommandDtoValidator : AbstractValidator<StockCommandDto>
{
    public StockCommandDtoValidator()
    {
        RuleFor(x => x.StockCode)
            .NotEmpty().WithMessage("Stock code is required")
            .MaximumLength(10)
            .Matches("^[a-zA-Z.]+$").WithMessage("Stock code must contain letters and optional dot");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MaximumLength(50);
    }
}

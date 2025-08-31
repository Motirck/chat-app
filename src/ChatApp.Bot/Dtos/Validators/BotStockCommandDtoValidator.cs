using FluentValidation;

namespace ChatApp.Bot.Dtos.Validators;

public class BotStockCommandDtoValidator : AbstractValidator<BotStockCommandDto>
{
    public BotStockCommandDtoValidator()
    {
        RuleFor(x => x.StockCode)
            .NotEmpty().WithMessage("Stock code is required")
            .MaximumLength(10)
            .Matches("^[a-zA-Z.]+$").WithMessage("Stock code must contain letters and optional dot");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MaximumLength(50);

        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("RoomId is required")
            .MaximumLength(50);
    }
}
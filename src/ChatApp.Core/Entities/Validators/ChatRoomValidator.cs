using FluentValidation;

namespace ChatApp.Core.Entities.Validators;

public class ChatRoomValidator : AbstractValidator<ChatRoom>
{
    public ChatRoomValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Room Id must contain only letters, numbers, dashes or underscores");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.CreatedAt)
            .NotEqual(default(DateTime));

        // IsActive is boolean, no validation needed beyond default
    }
}

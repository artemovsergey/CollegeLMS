using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class FeedbackRequestValidator : AbstractValidator<FeedbackRequest>
{
    public FeedbackRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно")
            .MaximumLength(100).WithMessage("Имя не должно превышать 100 символов");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .MaximumLength(255).WithMessage("Email не должен превышать 255 символов")
            .EmailAddress().WithMessage("Некорректный email");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Сообщение обязательно")
            .MaximumLength(4000).WithMessage("Сообщение не должно превышать 4000 символов");
    }
}

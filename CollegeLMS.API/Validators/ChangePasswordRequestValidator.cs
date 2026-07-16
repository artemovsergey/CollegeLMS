using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.OldPassword).NotEmpty().WithMessage("Старый пароль обязателен");
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Новый пароль обязателен")
            .MinimumLength(8)
            .WithMessage("Новый пароль должен содержать минимум 8 символов");
    }
}

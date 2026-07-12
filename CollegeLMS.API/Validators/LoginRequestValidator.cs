using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty()
            .WithMessage("Логин обязателен")
            .MaximumLength(100)
            .WithMessage("Логин не может быть длиннее 100 символов");

        RuleFor(x => x.Password).NotEmpty().WithMessage("Пароль обязателен");
    }
}

using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email обязателен")
            .EmailAddress()
            .WithMessage("Некорректный email")
            .MaximumLength(256)
            .WithMessage("Email не может быть длиннее 256 символов");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Пароль обязателен")
            .MinimumLength(6)
            .WithMessage("Пароль должен содержать минимум 6 символов")
            .MaximumLength(100)
            .WithMessage("Пароль не может быть длиннее 100 символов");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Имя обязательно")
            .MaximumLength(200)
            .WithMessage("Имя не может быть длиннее 200 символов");

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Некорректная роль")
            .NotEqual(Entities.Enums.UserRole.None)
            .WithMessage("Роль не может быть None");
    }
}

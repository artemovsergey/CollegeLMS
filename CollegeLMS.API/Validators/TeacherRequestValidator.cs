using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateTeacherRequestValidator : AbstractValidator<CreateTeacherRequest>
{
    public CreateTeacherRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email обязателен")
            .EmailAddress()
            .WithMessage("Некорректный email");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Пароль обязателен")
            .MinimumLength(6)
            .WithMessage("Пароль должен содержать минимум 6 символов");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("ФИО обязательно")
            .MaximumLength(200)
            .WithMessage("ФИО не должно превышать 200 символов");

        RuleFor(x => x.Department)
            .NotEmpty()
            .WithMessage("Кафедра обязательна")
            .MaximumLength(200)
            .WithMessage("Кафедра не должна превышать 200 символов");

        RuleFor(x => x.Position)
            .NotEmpty()
            .WithMessage("Должность обязательна")
            .MaximumLength(200)
            .WithMessage("Должность не должна превышать 200 символов");
    }
}

public class UpdateTeacherRequestValidator : AbstractValidator<UpdateTeacherRequest>
{
    public UpdateTeacherRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email обязателен")
            .EmailAddress()
            .WithMessage("Некорректный email");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("ФИО обязательно")
            .MaximumLength(200)
            .WithMessage("ФИО не должно превышать 200 символов");

        RuleFor(x => x.Department)
            .NotEmpty()
            .WithMessage("Кафедра обязательна")
            .MaximumLength(200)
            .WithMessage("Кафедра не должна превышать 200 символов");

        RuleFor(x => x.Position)
            .NotEmpty()
            .WithMessage("Должность обязательна")
            .MaximumLength(200)
            .WithMessage("Должность не должна превышать 200 символов");
    }
}

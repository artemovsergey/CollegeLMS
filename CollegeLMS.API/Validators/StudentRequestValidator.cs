using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    public CreateStudentRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(6).WithMessage("Пароль должен содержать минимум 6 символов");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("ФИО обязательно")
            .MaximumLength(200).WithMessage("ФИО не должно превышать 200 символов");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Группа обязательна");

        RuleFor(x => x.RecordBookNumber)
            .NotEmpty().WithMessage("Номер зачётной книжки обязателен")
            .MaximumLength(20).WithMessage("Номер зачётной книжки не должен превышать 20 символов");
    }
}

public class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный email");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("ФИО обязательно")
            .MaximumLength(200).WithMessage("ФИО не должно превышать 200 символов");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Группа обязательна");

        RuleFor(x => x.RecordBookNumber)
            .NotEmpty().WithMessage("Номер зачётной книжки обязателен")
            .MaximumLength(20).WithMessage("Номер зачётной книжки не должен превышать 20 символов");
    }
}

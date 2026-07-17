using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateScheduleRequestValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleRequestValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Название предмета обязательно")
            .MaximumLength(200)
            .WithMessage("Название предмета не должно превышать 200 символов");

        RuleFor(x => x.Room)
            .NotEmpty()
            .WithMessage("Номер аудитории обязателен")
            .MaximumLength(50)
            .WithMessage("Номер аудитории не должен превышать 50 символов");

        RuleFor(x => x.GroupId).NotEmpty().WithMessage("Группа обязательна");

        RuleFor(x => x.NumberPair)
            .InclusiveBetween(1, 8)
            .WithMessage("Номер пары должен быть от 1 до 8");

        RuleFor(x => x.StartTime)
            .LessThan(x => x.EndTime)
            .WithMessage("Время начала должно быть меньше времени окончания");

        RuleFor(x => x.Weeks)
            .NotEmpty()
            .WithMessage("Укажите хотя бы одну неделю");

        RuleFor(x => x.LessonType)
            .NotEmpty()
            .WithMessage("Тип занятия обязателен")
            .Must(BeValidLessonType)
            .WithMessage("Тип занятия должен быть одним из: Lecture, Practice, Lab, Exam");
    }

    private static bool BeValidLessonType(string value) => Enum.TryParse<LessonType>(value, out _);
}

public class UpdateScheduleRequestValidator : AbstractValidator<UpdateScheduleRequest>
{
    public UpdateScheduleRequestValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Название предмета обязательно")
            .MaximumLength(200)
            .WithMessage("Название предмета не должно превышать 200 символов");

        RuleFor(x => x.Room)
            .NotEmpty()
            .WithMessage("Номер аудитории обязателен")
            .MaximumLength(50)
            .WithMessage("Номер аудитории не должен превышать 50 символов");

        RuleFor(x => x.GroupId).NotEmpty().WithMessage("Группа обязательна");

        RuleFor(x => x.NumberPair)
            .InclusiveBetween(1, 8)
            .WithMessage("Номер пары должен быть от 1 до 8");

        RuleFor(x => x.StartTime)
            .LessThan(x => x.EndTime)
            .WithMessage("Время начала должно быть меньше времени окончания");

        RuleFor(x => x.Weeks)
            .NotEmpty()
            .WithMessage("Укажите хотя бы одну неделю");

        RuleFor(x => x.LessonType)
            .NotEmpty()
            .WithMessage("Тип занятия обязателен")
            .Must(BeValidLessonType)
            .WithMessage("Тип занятия должен быть одним из: Lecture, Practice, Lab, Exam");
    }

    private static bool BeValidLessonType(string value) => Enum.TryParse<LessonType>(value, out _);
}

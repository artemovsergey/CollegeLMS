using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Название занятия обязательно")
            .MaximumLength(255)
            .WithMessage("Название не должно превышать 255 символов");

        RuleFor(x => x.Content)
            .MaximumLength(65535)
            .WithMessage("Содержание не должно превышать 65535 символов");

        RuleFor(x => x.Kind)
            .Must(t => t is "Lecture" or "Practice" or "SelfStudy")
            .WithMessage("Недопустимый тип занятия");
    }
}

public class UpdateLessonRequestValidator : AbstractValidator<UpdateLessonRequest>
{
    public UpdateLessonRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Название занятия обязательно")
            .MaximumLength(255)
            .WithMessage("Название не должно превышать 255 символов");

        RuleFor(x => x.Content)
            .MaximumLength(65535)
            .WithMessage("Содержание не должно превышать 65535 символов");

        RuleFor(x => x.Kind)
            .Must(t => t is "Lecture" or "Practice" or "SelfStudy")
            .WithMessage("Недопустимый тип занятия");
    }
}

public class ReorderLessonsRequestValidator : AbstractValidator<ReorderLessonsRequest>
{
    public ReorderLessonsRequestValidator()
    {
        RuleFor(x => x.LessonIds)
            .NotEmpty()
            .WithMessage("Список занятий обязателен")
            .Must(x => x.Distinct().Count() == x.Count)
            .WithMessage("Список занятий не должен содержать дубликаты");
    }
}

public class UpdateLessonCurrentRequestValidator : AbstractValidator<UpdateLessonCurrentRequest>
{
    public UpdateLessonCurrentRequestValidator()
    {
        RuleFor(x => x.IsCurrent).NotNull().WithMessage("Поле isCurrent обязательно");
    }
}

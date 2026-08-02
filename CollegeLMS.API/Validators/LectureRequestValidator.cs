using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateLectureRequestValidator : AbstractValidator<CreateLectureRequest>
{
    public CreateLectureRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Название лекции обязательно")
            .MaximumLength(255)
            .WithMessage("Название не должно превышать 255 символов");

        RuleFor(x => x.Content)
            .MaximumLength(65535)
            .WithMessage("Содержание не должно превышать 65535 символов");

        RuleFor(x => x.LectureType)
            .Must(t => t is "Lecture" or "Practice" or "SelfStudy")
            .WithMessage("Недопустимый тип занятия");
    }
}

public class UpdateLectureRequestValidator : AbstractValidator<UpdateLectureRequest>
{
    public UpdateLectureRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Название лекции обязательно")
            .MaximumLength(255)
            .WithMessage("Название не должно превышать 255 символов");

        RuleFor(x => x.Content)
            .MaximumLength(65535)
            .WithMessage("Содержание не должно превышать 65535 символов");

        RuleFor(x => x.LectureType)
            .Must(t => t is "Lecture" or "Practice" or "SelfStudy")
            .WithMessage("Недопустимый тип занятия");
    }
}

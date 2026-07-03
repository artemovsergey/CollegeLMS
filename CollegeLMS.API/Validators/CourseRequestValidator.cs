using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название курса обязательно")
            .MaximumLength(255).WithMessage("Название не должно превышать 255 символов");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Описание не должно превышать 4000 символов");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Группа обязательна");
    }
}

public class UpdateCourseRequestValidator : AbstractValidator<UpdateCourseRequest>
{
    public UpdateCourseRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название курса обязательно")
            .MaximumLength(255).WithMessage("Название не должно превышать 255 символов");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Описание не должно превышать 4000 символов");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Группа обязательна");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Статус обязателен")
            .Must(s => s is "Draft" or "Active" or "Archived")
            .WithMessage("Статус должен быть Draft, Active или Archived");
    }
}

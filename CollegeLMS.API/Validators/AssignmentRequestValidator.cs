using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateAssignmentRequestValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Название задания обязательно")
            .MaximumLength(255)
            .WithMessage("Название не должно превышать 255 символов");

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .WithMessage("Описание не должно превышать 4000 символов");

        RuleFor(x => x.MaxScore)
            .InclusiveBetween(0, 1000)
            .WithMessage("Максимальный балл должен быть от 0 до 1000");
    }
}

public class UpdateAssignmentRequestValidator : AbstractValidator<UpdateAssignmentRequest>
{
    public UpdateAssignmentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Название задания обязательно")
            .MaximumLength(255)
            .WithMessage("Название не должно превышать 255 символов");

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .WithMessage("Описание не должно превышать 4000 символов");

        RuleFor(x => x.MaxScore)
            .InclusiveBetween(0, 1000)
            .WithMessage("Максимальный балл должен быть от 0 до 1000");
    }
}

using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название группы обязательно")
            .MaximumLength(100).WithMessage("Название группы не должно превышать 100 символов");

        RuleFor(x => x.Course)
            .InclusiveBetween(1, 4).WithMessage("Курс должен быть от 1 до 4");
    }
}

public class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название группы обязательно")
            .MaximumLength(100).WithMessage("Название группы не должно превышать 100 символов");

        RuleFor(x => x.Course)
            .InclusiveBetween(1, 4).WithMessage("Курс должен быть от 1 до 4");
    }
}

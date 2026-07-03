using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Некорректная роль")
            .NotEqual(Entities.Enums.UserRole.None)
            .WithMessage("Роль не может быть None");
    }
}

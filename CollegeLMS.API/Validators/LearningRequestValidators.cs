using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateSpecialtyRequestValidator : AbstractValidator<CreateSpecialtyRequest>
{
    public CreateSpecialtyRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Код специальности обязателен")
            .MaximumLength(50);
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Название специальности обязательно")
            .MaximumLength(255);
        RuleFor(x => x.Department)
            .NotEmpty()
            .WithMessage("Отделение обязательно")
            .MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(4000);
    }
}

public class UpdateSpecialtyRequestValidator : AbstractValidator<UpdateSpecialtyRequest>
{
    public UpdateSpecialtyRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Код специальности обязателен")
            .MaximumLength(50);
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Название специальности обязательно")
            .MaximumLength(255);
        RuleFor(x => x.Department)
            .NotEmpty()
            .WithMessage("Отделение обязательно")
            .MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(4000);
    }
}

public class TransferStudentRequestValidator : AbstractValidator<TransferStudentRequest>
{
    public TransferStudentRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Причина перевода обязательна")
            .MaximumLength(1000);
    }
}

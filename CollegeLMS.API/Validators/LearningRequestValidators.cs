using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateSemesterRequestValidator : AbstractValidator<CreateSemesterRequest>
{
    public CreateSemesterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Название семестра обязательно")
            .MaximumLength(255);
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Дата начала обязательна");
        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("Дата конца обязательна")
            .GreaterThan(x => x.StartDate)
            .WithMessage("Дата конца должна быть позже даты начала");
        RuleFor(x => x.Type).NotEmpty().WithMessage("Тип семестра обязателен");
        RuleFor(x => x.AcademicYear)
            .NotEmpty()
            .WithMessage("Учебный год обязателен")
            .MaximumLength(50);
    }
}

public class UpdateSemesterRequestValidator : AbstractValidator<UpdateSemesterRequest>
{
    public UpdateSemesterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Название семестра обязательно")
            .MaximumLength(255);
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Дата начала обязательна");
        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("Дата конца обязательна")
            .GreaterThan(x => x.StartDate)
            .WithMessage("Дата конца должна быть позже даты начала");
        RuleFor(x => x.Type).NotEmpty().WithMessage("Тип семестра обязателен");
        RuleFor(x => x.AcademicYear)
            .NotEmpty()
            .WithMessage("Учебный год обязателен")
            .MaximumLength(50);
    }
}

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

public class CreateExamRequestValidator : AbstractValidator<CreateExamRequest>
{
    public CreateExamRequestValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().WithMessage("Предмет обязателен").MaximumLength(255);
        RuleFor(x => x.ExamDate).NotEmpty().WithMessage("Дата экзамена обязательна");
        RuleFor(x => x.Type).NotEmpty().WithMessage("Тип экзамена обязателен");
    }
}

public class UpdateExamRequestValidator : AbstractValidator<UpdateExamRequest>
{
    public UpdateExamRequestValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().WithMessage("Предмет обязателен").MaximumLength(255);
        RuleFor(x => x.ExamDate).NotEmpty().WithMessage("Дата экзамена обязательна");
        RuleFor(x => x.Type).NotEmpty().WithMessage("Тип экзамена обязателен");
        RuleFor(x => x.Status).NotEmpty().WithMessage("Статус обязателен");
    }
}

public class CreateRetakeRequestValidator : AbstractValidator<CreateRetakeRequest>
{
    public CreateRetakeRequestValidator()
    {
        RuleFor(x => x.RetakeDate).NotEmpty().WithMessage("Дата пересдачи обязательна");
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Причина пересдачи обязательна")
            .MaximumLength(1000);
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

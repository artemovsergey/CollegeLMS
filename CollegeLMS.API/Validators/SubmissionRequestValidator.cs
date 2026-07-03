using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class SubmitAssignmentRequestValidator : AbstractValidator<SubmitAssignmentRequest>
{
    public SubmitAssignmentRequestValidator()
    {
        RuleFor(x => x.FilePath)
            .NotEmpty().WithMessage("Путь к файлу обязателен")
            .MaximumLength(500).WithMessage("Путь к файлу не должен превышать 500 символов");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Комментарий не должен превышать 1000 символов");
    }
}

public class GradeSubmissionRequestValidator : AbstractValidator<GradeSubmissionRequest>
{
    public GradeSubmissionRequestValidator()
    {
        RuleFor(x => x.Score)
            .InclusiveBetween(0, 1000).WithMessage("Оценка должна быть от 0 до 1000");
    }
}

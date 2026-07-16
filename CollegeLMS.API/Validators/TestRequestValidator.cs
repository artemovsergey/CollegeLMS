using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateTestRequestValidator : AbstractValidator<CreateTestRequest>
{
    public CreateTestRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Название теста обязательно")
            .MaximumLength(255).WithMessage("Название теста не должно превышать 255 символов");
        RuleFor(x => x.TimeLimitMinutes).GreaterThan(0).WithMessage("Время на прохождение должно быть больше 0");
        RuleFor(x => x.MaxAttempts).GreaterThan(0).WithMessage("Количество попыток должно быть больше 0");
        RuleFor(x => x.Type).NotEmpty().WithMessage("Тип теста обязателен");
        RuleFor(x => x.PassingScore).InclusiveBetween(0, 100).WithMessage("Проходной балл должен быть от 0 до 100");
    }
}

public class UpdateTestRequestValidator : AbstractValidator<UpdateTestRequest>
{
    public UpdateTestRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Название теста обязательно")
            .MaximumLength(255).WithMessage("Название теста не должно превышать 255 символов");
        RuleFor(x => x.TimeLimitMinutes).GreaterThan(0).WithMessage("Время на прохождение должно быть больше 0");
        RuleFor(x => x.MaxAttempts).GreaterThan(0).WithMessage("Количество попыток должно быть больше 0");
        RuleFor(x => x.Type).NotEmpty().WithMessage("Тип теста обязателен");
        RuleFor(x => x.PassingScore).InclusiveBetween(0, 100).WithMessage("Проходной балл должен быть от 0 до 100");
    }
}

public class CreateQuestionRequestValidator : AbstractValidator<CreateQuestionRequest>
{
    public CreateQuestionRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage("Текст вопроса обязателен")
            .MaximumLength(4000).WithMessage("Текст вопроса не должен превышать 4000 символов");
        RuleFor(x => x.Type).NotEmpty().WithMessage("Тип вопроса обязателен");
        RuleFor(x => x.Points).GreaterThan(0).WithMessage("Баллы должны быть больше 0");
    }
}

public class UpdateQuestionRequestValidator : AbstractValidator<UpdateQuestionRequest>
{
    public UpdateQuestionRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage("Текст вопроса обязателен")
            .MaximumLength(4000).WithMessage("Текст вопроса не должен превышать 4000 символов");
        RuleFor(x => x.Type).NotEmpty().WithMessage("Тип вопроса обязателен");
        RuleFor(x => x.Points).GreaterThan(0).WithMessage("Баллы должны быть больше 0");
    }
}

public class AssignTestRequestValidator : AbstractValidator<AssignTestRequest>
{
    public AssignTestRequestValidator()
    {
        RuleFor(x => x.OpenDate).NotEmpty().WithMessage("Дата открытия обязательна");
        RuleFor(x => x.CloseDate).NotEmpty().WithMessage("Дата закрытия обязательна")
            .GreaterThan(x => x.OpenDate).WithMessage("Дата закрытия должна быть позже даты открытия");
        RuleFor(x => x.MaxAttempts).GreaterThan(0).WithMessage("Количество попыток должно быть больше 0");
    }
}

public class TestSettingsRequestValidator : AbstractValidator<TestSettingsRequest>
{
    public TestSettingsRequestValidator()
    {
        When(x => x.PassingScore.HasValue, () =>
        {
            RuleFor(x => x.PassingScore!.Value).InclusiveBetween(0, 100)
                .WithMessage("Проходной балл должен быть от 0 до 100");
        });
    }
}

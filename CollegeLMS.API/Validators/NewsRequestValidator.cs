using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateNewsRequestValidator : AbstractValidator<CreateNewsRequest>
{
    public CreateNewsRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Заголовок новости обязателен")
            .MaximumLength(255)
            .WithMessage("Заголовок не должен превышать 255 символов");

        RuleFor(x => x.Content).NotEmpty().WithMessage("Содержание новости обязательно");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048)
            .WithMessage("URL изображения не должен превышать 2048 символов");
    }
}

public class UpdateNewsRequestValidator : AbstractValidator<UpdateNewsRequest>
{
    public UpdateNewsRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Заголовок новости обязателен")
            .MaximumLength(255)
            .WithMessage("Заголовок не должен превышать 255 символов");

        RuleFor(x => x.Content).NotEmpty().WithMessage("Содержание новости обязательно");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048)
            .WithMessage("URL изображения не должен превышать 2048 символов");
    }
}

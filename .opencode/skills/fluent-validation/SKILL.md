---
name: fluent-validation
description: Create FluentValidation validators and register them in DI
---

# fluent-validation

Create FluentValidation validators for request DTOs and register them in the DI container.

## Files

### Validator class

Path: `CollegeLMS.API/Validators/{Action}{Name}RequestValidator.cs`

```csharp
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class Create{Name}RequestValidator : AbstractValidator<Create{Name}Request>
{
    public Create{Name}RequestValidator()
    {
        RuleFor(x => x.{Property})
            .NotEmpty().WithMessage("Поле обязательно")
            .MaximumLength({n}).WithMessage("Максимум {n} символов");

        RuleFor(x => x.{EmailProperty})
            .EmailAddress().WithMessage("Некорректный email");

        RuleFor(x => x.{EnumProperty})
            .IsInEnum().WithMessage("Некорректное значение");
    }
}
```

### DI Registration in Program.cs

```csharp
using FluentValidation;

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

## Convention rules

- One validator per request DTO
- Error messages in Russian
- Use `RuleFor` chains with `.NotEmpty()`, `.MaximumLength()`, `.EmailAddress()`, `.IsInEnum()`
- Register all validators at once with `AddValidatorsFromAssemblyContaining<Program>()`

## Verification

- `dotnet build` succeeds
- Invalid requests return 400 with validation errors

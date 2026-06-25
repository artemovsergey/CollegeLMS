# SKILL: CQRS + MediatR в ASP.NET Core

## Когда использовать этот скилл

Применяй этот скилл при любой задаче, связанной с:
- Реализацией CQRS (Command/Query Responsibility Segregation) в .NET
- Использованием библиотеки MediatR (IRequest, IRequestHandler, INotification)
- Созданием Pipeline Behaviors (логирование, валидация, обработка исключений)
- Интеграцией FluentValidation с MediatR
- Вертикальными срезами (Vertical Slice Architecture / VSA)
- Запросами из репозитория `artemovsergey/.NET` (Wiki: CQRS, Mediatr, VSA, FluentValidation)

---

## Источники знаний

| Репозиторий | Что использует |
|---|---|
| `artemovsergey/.NET` (Wiki/CQRS, Wiki/Mediatr, Wiki/VSA) | Паттерны Request/Handler, Behaviors |
| `artemovsergey/ModuleBankApp` | MediatR в микросервисе счетов |
| `artemovsergey/SampleApp` | CQRS в multi-targeting проекте |

---

## 1. Структура Request + Handler

Каждый сценарий описывается через **вложенный record**. Request и его Response живут в одном файле.

```csharp
// Команда (Command)
public record CreateAccountCommand(string OwnerId, decimal InitialBalance)
    : IRequest<CreateAccountCommand.Response>
{
    public const string RouteTemplate = "api/accounts";
    public record Response(Guid AccountId, decimal Balance);
}

// Запрос (Query)
public record GetAccountQuery(Guid AccountId)
    : IRequest<GetAccountQuery.Response>
{
    public record Response(Guid Id, string OwnerId, decimal Balance, bool IsActive);
}
```

### Handler

```csharp
public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, CreateAccountCommand.Response>
{
    private readonly AppDbContext _db;
    private readonly ILogger<CreateAccountHandler> _logger;

    public CreateAccountHandler(AppDbContext db, ILogger<CreateAccountHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CreateAccountCommand.Response> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = new Account
        {
            OwnerId = request.OwnerId,
            Balance = request.InitialBalance,
            IsActive = true
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Account {Id} created for owner {OwnerId}", account.Id, account.OwnerId);

        return new CreateAccountCommand.Response(account.Id, account.Balance);
    }
}
```

---

## 2. Pipeline Behaviors

Behaviors добавляются в **пайплайн MediatR** и выполняются для каждого запроса.

### Logging Behavior (Pre-processor)

```csharp
public class LoggingBehavior<TRequest> : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest>> logger) => _logger = logger;

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("[MediatR] Handling {RequestName}: {@Request}", requestName, request);
        return Task.CompletedTask;
    }
}
```

### Performance Behavior

```csharp
public class PerformanceBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly Stopwatch _timer = new();

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();
        var response = await next();
        _timer.Stop();

        var elapsed = _timer.ElapsedMilliseconds;
        if (elapsed > 500)
        {
            _logger.LogWarning("[Perf] Slow request {Name} took {Elapsed}ms: {@Request}",
                typeof(TRequest).Name, elapsed, request);
        }

        return response;
    }
}
```

### Unhandled Exception Behavior

```csharp
public class UnhandledExceptionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> _logger;

    public UnhandledExceptionBehavior(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Exception] {RequestName} failed: {@Request}",
                typeof(TRequest).Name, request);
            throw;
        }
    }
}
```

### Validation Behavior (с FluentValidation)

```csharp
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

---

## 3. FluentValidation — Validator для Request

```csharp
public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("OwnerId is required")
            .MaximumLength(50);

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0).WithMessage("Balance cannot be negative");
    }
}
```

---

## 4. Регистрация в Program.cs

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

    // Порядок behaviors важен!
    cfg.AddRequestPreProcessor(typeof(LoggingBehavior<>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
```

---

## 5. Minimal API + MediatR (из TicTacToe / SampleApp)

```csharp
// Endpoint группы как extension
public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts");

        group.MapPost("/", async (CreateAccountCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return Results.Created($"/api/accounts/{result.AccountId}", result);
        })
        .WithName("CreateAccount")
        .Produces<CreateAccountCommand.Response>(StatusCodes.Status201Created);

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetAccountQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetAccount");
    }
}
```

---

## 6. Vertical Slice Architecture (VSA)

Структура проекта по **фичам** (не по слоям):

```
Features/
  Accounts/
    Create/
      CreateAccountCommand.cs       ← Request + Response
      CreateAccountHandler.cs       ← Handler
      CreateAccountValidator.cs     ← Validator
      CreateAccountEndpoint.cs      ← Minimal API endpoint
    GetById/
      GetAccountQuery.cs
      GetAccountQueryHandler.cs
    Shared/
      AccountDto.cs
```

Каждый slice самодостаточен и не зависит от других.

---

## 7. Notifications (Domain Events)

```csharp
// Событие
public record AccountCreatedEvent(Guid AccountId, string OwnerId) : INotification;

// Обработчик 1: отправить email
public class SendWelcomeEmailHandler : INotificationHandler<AccountCreatedEvent>
{
    public async Task Handle(AccountCreatedEvent notification, CancellationToken ct)
    {
        // логика отправки email
    }
}

// Обработчик 2: создать аудит-запись
public class CreateAuditRecordHandler : INotificationHandler<AccountCreatedEvent>
{
    public async Task Handle(AccountCreatedEvent notification, CancellationToken ct)
    {
        // логика аудита
    }
}

// Публикация в Handler
await _mediator.Publish(new AccountCreatedEvent(account.Id, account.OwnerId), cancellationToken);
```

---

## Checklist перед генерацией кода

- [ ] Request реализует `IRequest<Response>`
- [ ] Response — вложенный `record` внутри Request
- [ ] Handler инжектирует только нужные зависимости
- [ ] Validator наследует `AbstractValidator<TRequest>`
- [ ] Behaviors зарегистрированы в правильном порядке (Logging → Exception → Validation → Performance)
- [ ] В Minimal API используется `IMediator` или `ISender`
- [ ] Для domain events — `INotification` + один или несколько `INotificationHandler`

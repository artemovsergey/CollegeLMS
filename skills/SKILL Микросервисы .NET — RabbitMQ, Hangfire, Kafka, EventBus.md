# SKILL: Микросервисы .NET — RabbitMQ, Hangfire, Kafka, EventBus

## Когда использовать этот скилл

Применяй этот скилл при задачах:
- Проектирование микросервисов на .NET (отдельные сервисы по bounded context)
- Асинхронный обмен сообщениями через RabbitMQ или Kafka
- Фоновые задачи с Hangfire (recurring, delayed, fire-and-forget jobs)
- EventBus — публикация/подписка на доменные события между сервисами
- Docker Compose для локального запуска инфраструктуры

Источники: `artemovsergey/ModuleBankApp`, `artemovsergey/.NET` (Wiki/Microservices, Wiki/Hangfire, Wiki/Kafka, Wiki/EventBus, Wiki/Docker-compose)

---

## 1. RabbitMQ — отправка и получение сообщений

### Установка

```bash
dotnet add package RabbitMQ.Client
```

### Конфигурация (appsettings.json)

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

### Publisher (отправитель)

```csharp
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string exchange, string routingKey) where T : class;
}

public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqPublisher(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"],
            Port     = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:Username"],
            Password = config["RabbitMQ:Password"]
        };
        _connection = factory.CreateConnection();
        _channel    = _connection.CreateModel();
    }

    public Task PublishAsync<T>(T message, string exchange, string routingKey) where T : class
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = _channel.CreateBasicProperties();
        props.Persistent  = true;
        props.ContentType = "application/json";

        _channel.BasicPublish(exchange, routingKey, props, body);
        return Task.CompletedTask;
    }

    public void Dispose() { _channel?.Dispose(); _connection?.Dispose(); }
}
```

### Consumer (BackgroundService)

```csharp
public class AccountCreatedConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AccountCreatedConsumer> _logger;
    private IConnection _connection;
    private IModel _channel;

    public AccountCreatedConsumer(IServiceProvider services, ILogger<AccountCreatedConsumer> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = _services.GetRequiredService<IConfiguration>();
        // ... инициализация connection/channel ...

        _channel.QueueDeclare("account.created", durable: true, exclusive: false, autoDelete: false);
        _channel.BasicQos(0, 1, false); // prefetch count = 1

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var body    = ea.Body.ToArray();
            var message = JsonSerializer.Deserialize<AccountCreatedEvent>(body);

            using var scope = _services.CreateScope();
            var mediator    = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Publish(message!, stoppingToken);
            _channel.BasicAck(ea.DeliveryTag, false);

            _logger.LogInformation("Processed AccountCreatedEvent for {AccountId}", message?.AccountId);
        };

        _channel.BasicConsume("account.created", autoAck: false, consumer);
        return Task.CompletedTask;
    }
}
```

### Регистрация

```csharp
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<AccountCreatedConsumer>();
```

---

## 2. Hangfire — фоновые задачи

### Установка

```bash
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql  # или Hangfire.SqlServer
```

### Конфигурация

```csharp
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("Hangfire")));

builder.Services.AddHangfireServer(opts =>
{
    opts.WorkerCount = Environment.ProcessorCount * 2;
    opts.Queues = new[] { "critical", "default", "low" };
});

// Dashboard (только в Development)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AllowAllConnectionsFilter() }
});
```

### Виды задач

```csharp
public class PaymentJobService
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IRecurringJobManager _recurringJobs;

    public PaymentJobService(IBackgroundJobClient jobClient, IRecurringJobManager recurringJobs)
    {
        _jobClient    = jobClient;
        _recurringJobs = recurringJobs;
    }

    // Fire-and-forget: выполнить как можно скорее
    public void EnqueuePaymentProcessing(Guid paymentId)
        => _jobClient.Enqueue<IPaymentProcessor>(p => p.Process(paymentId));

    // Delayed: выполнить через 30 минут
    public void SchedulePaymentReminder(Guid paymentId)
        => _jobClient.Schedule<INotificationService>(
            n => n.SendReminder(paymentId),
            TimeSpan.FromMinutes(30));

    // Recurring: каждый час
    public void RegisterHourlyStatistics()
        => _recurringJobs.AddOrUpdate<IStatisticsService>(
            "hourly-stats",
            s => s.CalculateHourlyStats(),
            Cron.Hourly);

    // Continuations: цепочка задач
    public void EnqueuePaymentChain(Guid paymentId)
    {
        var jobId = _jobClient.Enqueue<IPaymentProcessor>(p => p.Validate(paymentId));
        _jobClient.ContinueJobWith<IPaymentProcessor>(jobId, p => p.Process(paymentId));
    }
}
```

### MediatR + Hangfire (dispatch через очередь)

```csharp
public static class MediatorExtensions
{
    // Откладывает выполнение MediatR-команды через Hangfire
    public static string EnqueueCommand<TCommand>(
        this IBackgroundJobClient client,
        TCommand command) where TCommand : IBaseRequest
    {
        return client.Enqueue<IMediator>(m => m.Send(command, CancellationToken.None));
    }
}

// Использование в контроллере/endpoint:
_backgroundJobClient.EnqueueCommand(new ProcessPaymentCommand(paymentId));
```

---

## 3. Kafka — продюсер и потребитель

### Установка

```bash
dotnet add package Confluent.Kafka
```

### Producer

```csharp
public class KafkaProducer<TKey, TValue> : IDisposable
{
    private readonly IProducer<TKey, TValue> _producer;

    public KafkaProducer(IConfiguration config)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            Acks = Acks.All
        };
        _producer = new ProducerBuilder<TKey, TValue>(producerConfig)
            .SetValueSerializer(new JsonSerializer<TValue>())
            .Build();
    }

    public async Task ProduceAsync(string topic, TKey key, TValue value)
    {
        var message = new Message<TKey, TValue> { Key = key, Value = value };
        await _producer.ProduceAsync(topic, message);
    }

    public void Dispose() => _producer?.Dispose();
}
```

### Consumer (BackgroundService)

```csharp
public class KafkaConsumerService<TKey, TValue> : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly Func<TValue, Task> _handler;

    public KafkaConsumerService(IConfiguration config, Func<TValue, Task> handler)
    {
        _config  = config;
        _handler = handler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId          = _config["Kafka:GroupId"],
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<TKey, TValue>(consumerConfig)
            .SetValueDeserializer(new JsonDeserializer<TValue>())
            .Build();

        consumer.Subscribe(_config["Kafka:Topic"]);

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = consumer.Consume(stoppingToken);
            await _handler(result.Message.Value);
            consumer.Commit(result);
        }
    }
}
```

---

## 4. EventBus (внутренний, in-memory)

```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;
    void Subscribe<TEvent, THandler>()
        where TEvent : class
        where THandler : IEventHandler<TEvent>;
}

public interface IEventHandler<in TEvent>
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}

// Простая in-memory реализация через словарь
public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _services;
    private readonly Dictionary<Type, List<Type>> _handlers = new();

    public InMemoryEventBus(IServiceProvider services) => _services = services;

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (!_handlers.TryGetValue(eventType, out var handlerTypes)) return;

        using var scope = _services.CreateScope();
        foreach (var handlerType in handlerTypes)
        {
            var handler = (IEventHandler<TEvent>)scope.ServiceProvider.GetRequiredService(handlerType);
            await handler.HandleAsync(@event, ct);
        }
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : class
        where THandler : IEventHandler<TEvent>
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType)) _handlers[eventType] = new List<Type>();
        _handlers[eventType].Add(typeof(THandler));
    }
}
```

---

## 5. Docker Compose — инфраструктура

```yaml
# docker-compose.yml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  kafka:
    image: confluentinc/cp-kafka:7.4.0
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
    depends_on:
      - zookeeper

  zookeeper:
    image: confluentinc/cp-zookeeper:7.4.0
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181

  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: bankdb
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: secret
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  rabbitmq_data:
  postgres_data:
```

---

## 6. Keycloak — OAuth2/OIDC (из ModuleBankApp)

```yaml
  keycloak:
    image: quay.io/keycloak/keycloak:24.0
    command: start-dev
    ports:
      - "8080:8080"
    environment:
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://postgres:5432/keycloakdb
      KC_DB_USERNAME: admin
      KC_DB_PASSWORD: secret
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
```

```csharp
// Защита API с Keycloak в .NET
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience  = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false; // только в dev
    });
```

---

## Checklist для микросервиса

- [ ] Каждый сервис имеет **один** bounded context и свою базу данных
- [ ] Сервисы общаются асинхронно (RabbitMQ/Kafka), не через прямые HTTP-вызовы
- [ ] Hangfire использует **отдельную** БД или схему
- [ ] Consumer реализован как `BackgroundService` и правильно обрабатывает `stoppingToken`
- [ ] Сообщения сериализуются в JSON с именами типов для десериализации
- [ ] Docker Compose содержит healthcheck для всех инфраструктурных сервисов
- [ ] Нет прямой зависимости от типов других микросервисов (только shared contracts)

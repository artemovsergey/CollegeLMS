---
name: dotnet-entity
description: Create a new domain entity with EF Core configuration, base Entity inheritance, enum, HasData, raw SQL constraints, and migration
---

# dotnet-entity

Create a domain entity class with EF Core Fluent API configuration, following CollegeLMS conventions.

## Workflow

### 1. Create entity class

Path: `CollegeLMS.API/Entities/{Name}.cs`

```csharp
using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class {Name} : Entity
{
    public string Property { get; set; } = string.Empty;
    // navigation — if needed
    // [JsonIgnore]
    // public RelatedEntity Related { get; set; } = null!;
}
```

Base `Entity` class lives at `CollegeLMS.API/Entities/Entity.cs`:

```csharp
namespace CollegeLMS.API.Entities;

public abstract class Entity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

All entities MUST inherit from `Entity`. PK (`Guid Id`), `CreatedAt`, `UpdatedAt` are inherited.

- Guid PK — `ValueGeneratedNever()` in config
- Navigation properties get `[JsonIgnore]` to avoid cycles
- FK property follows convention: `{RelatedEntityName}Id`
- `CancellationToken ct` on all async methods

### 2. Create enum if needed

Path: `CollegeLMS.API/Entities/Enums/{Name}Type.cs`

```csharp
namespace CollegeLMS.API.Entities.Enums;

public enum {Name}Type
{
    None = 0,
    // values...
}
```

### 3. Create EF Core configuration (schema only)

Path: `CollegeLMS.API/Data/Configurations/{Name}Configuration.cs`

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class {Name}Configuration : IEntityTypeConfiguration<{Name}>
{
    public void Configure(EntityTypeBuilder<{Name}> builder)
    {
        builder.ToTable("{table_name}");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Property).HasMaxLength({n});

        builder.Property(x => x.EnumProperty)
            .HasConversion<string>()
            .HasMaxLength({n});

        // Navigation
        builder.HasOne(x => x.Navigation)
            .WithMany(x => x.Collection)
            .HasForeignKey(x => x.ForeignKeyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes (EF manages these via migrations)
        builder.HasIndex(x => x.Property).IsUnique().HasDatabaseName("ix_{table}_{column}");
        builder.HasIndex(x => new { x.PropA, x.PropB })
            .IsUnique()
            .HasDatabaseName("ix_{table}_prop_a_prop_b");
        builder.HasIndex(x => x.ForeignId).HasDatabaseName("ix_{table}_foreign_id");

        // CHECK constraints → NOT here, see Data/DbConstraints.cs

        // HasData — тестовые данные для разработки
        builder.HasData(
            new {Name}
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Property = "test value",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            }
        );
    }
}
```

**Важно**: EF Configuration отвечает ТОЛЬКО за схему (типы колонок, длины, FK, индексы).
CHECK constraints — в `Data/DbConstraints.cs` (см. шаг 6).

### 4. Register in DbContext

`CollegeLMS.API/Data/AppDbContext.cs`:

```csharp
public DbSet<{Name}> {PluralName} { get; set; }
```

No manual `ApplyConfiguration` needed — `ApplyConfigurationsFromAssembly` in `OnModelCreating` discovers all configs automatically.

### 5. Create migration

```powershell
dotnet ef migrations add Add{Name}Entity --project CollegeLMS.API -- --provider Npgsql
dotnet ef database update --project CollegeLMS.API
```

### 6. Add CHECK constraints via DbConstraints.cs

Path: `CollegeLMS.API/Data/DbConstraints.cs`

CHECK constraints добавляются отдельно от миграций — идемпотентным SQL, который выполняется при старте приложения.

Дописать в существующий метод `EnsureAsync`:

```csharp
public static class DbConstraints
{
    public static async Task EnsureAsync(AppDbContext db)
    {
        var sql = """

        -- {Name}
        DO $$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_{table}_{column}') THEN
                ALTER TABLE {table} ADD CONSTRAINT ck_{table}_{column}
                    CHECK (condition);
            END IF;
        END $$;

        """;

        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
```

Паттерн:
- `DO $$ ... END $$` — анонимный PL/pgSQL блок
- `IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = '...')` — идемпотентность
- Все constraint-ы именуются с префиксом `ck_`

`EnsureAsync` вызывается автоматически в `ApplicationBuilderExtensions.MigrateDatabaseAsync()`
после миграций и seed-данных:

```csharp
await db.Database.MigrateAsync();
await DataSeeder.SeedAsync(db);
await DbConstraints.EnsureAsync(db);
```

## Convention rules

- Table name: snake_case plural (`users`, `schedule_entries`)
- Column names: snake_case via `UseSnakeCaseNamingConvention()`
- String properties MUST have `HasMaxLength()`
- Enum properties MUST have `HasConversion<string>()` + `HasMaxLength()`
- Navigation properties: `[JsonIgnore]`
- Timestamps: inherited from `Entity` — `CreatedAt`, `UpdatedAt`
- **Indexes** (UNIQUE, plain) — in EF Configuration via `HasIndex()`
- **CHECK constraints** — in `Data/DbConstraints.cs` via idempotent PL/pgSQL (`DO $$ IF NOT EXISTS`)
- `HasIndex` names: `ix_{table}_{column}`
- `Check` names: `ck_{table}_{column}`
- HasData: include test seed data directly in configuration

## Verification

- `dotnet build` succeeds
- Migration file generated in `Migrations/` with proper Up/Down (schema only, no CHECK)
- Migration includes `HasData()` INSERT statements
- `dotnet ef database update` applies without errors
- `DbConstraints.EnsureAsync` creates constraints idempotently (running twice is safe)
- Constraints visible in pgAdmin/psql: `\d+ {table}`

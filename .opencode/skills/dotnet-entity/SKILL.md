---
name: dotnet-entity
description: Create a new domain entity with EF Core configuration, enum, and migration
---

# dotnet-entity

Create a domain entity class with EF Core Fluent API configuration, following CollegeLMS conventions.

## Workflow

### 1. Create entity class

Path: `CollegeLMS.API/Entities/{Name}.cs`

```csharp
namespace CollegeLMS.API.Entities;

public class {Name}
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- `Guid` for PK — `ValueGeneratedNever()` in config
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

### 3. Create EF Core configuration

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

        builder.Property(x => x.{property}).HasMaxLength({n});

        builder.Property(x => x.{enumProperty})
            .HasConversion<string>()
            .HasMaxLength({n});

        builder.HasOne(x => x.{Navigation})
            .WithMany(x => x.{Collection})
            .HasForeignKey(x => x.{ForeignKeyId})
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 4. Register in DbContext

`CollegeLMS.API/Data/AppDbContext.cs`:

```csharp
public DbSet<{Name}> {PluralName} { get; set; }
```

In `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new {Name}Configuration());
```

Also configure snake_case globally in DbContext constructor or `OnModelCreating`:
```csharp
// In OnModelCreating
builder.HasPostgresExtension("uuid-ossp");
// Snake-case naming convention via NuGet: EFCore.NamingConventions
// Register in Program.cs: options.UseSnakeCaseNamingConvention()
```

### 5. Create migration

```powershell
dotnet ef migrations add Add{Name}Entity -- --provider Npgsql
dotnet ef database update
```

## NuGet packages (reference)

| Package | Purpose |
|---------|---------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Postgres provider for EF Core |
| `Microsoft.EntityFrameworkCore.Design` | EF tools (migrations) |
| `Microsoft.EntityFrameworkCore.Tools` | EF CLI commands |
| `EFCore.NamingConventions` | Snake_case naming |
| `AspNetCore.HealthChecks.NpgSql` | DB health check |

## Convention rules

- Table name: snake_case plural (`users`, `schedule_entries`)
- Column names: snake_case via `UseSnakeCaseNamingConvention()`
- String properties MUST have `HasMaxLength()`
- Enum properties MUST have `HasConversion<string>()`
- Navigation properties: `[JsonIgnore]`
- Timestamps: `CreatedAt`, `UpdatedAt` — `DateTime.UtcNow`

## Verification

- `dotnet build` succeeds
- Migration file generated in `Migrations/` with proper Up/Down
- `dotnet ef database update` applies without errors

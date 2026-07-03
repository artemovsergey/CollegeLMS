---
name: seed-data
description: Create EF Core seed data using HasData or custom ISeedService
---

# seed-data

Create seed data for development or initial database population.

## Option 1: EF Core HasData (in Configuration)

```csharp
public sealed class {Name}Configuration : IEntityTypeConfiguration<{Name}>
{
    public void Configure(EntityTypeBuilder<{Name}> builder)
    {
        builder.ToTable("{table_name}");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasData(
            new {Name}
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                // fill required fields
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            }
        );
    }
}
```

## Option 2: Static Seed class

Path: `CollegeLMS.API/Data/Seed/{Name}Seed.cs`

```csharp
namespace CollegeLMS.API.Data.Seed;

public static class {Name}Seed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<{Name}>().HasData(
            new {Name} { /* ... */ }
        );
    }
}
```

Register in `AppDbContext.OnModelCreating`:
```csharp
{Name}Seed.Seed(modelBuilder);
```

## Option 3: Program.cs DataSeeder

For bulk import (WordPress migration):

```csharp
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        db.Users.AddRange(
            new User { /* ... */ }
        );
        await db.SaveChangesAsync();
    }
}
```

Call in Program.cs:
```csharp
await DataSeeder.SeedAsync(db);
```

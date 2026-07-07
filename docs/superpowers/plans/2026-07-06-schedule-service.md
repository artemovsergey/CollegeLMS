# ScheduleService Implementation Plan

> **For agentic workers:** Implement ScheduleService — entity, API, tests, frontend. Full vertical slice.

**Goal:** ScheduleEntry entity + CRUD + filter API + frontend pages

**Architecture:** Clean Architecture — Entity → EF Config → Service → Controller → DTOs. Frontend pages in Next.js App Router.

**Tech Stack:** .NET 10, EF Core + Npgsql, FluentValidation, xUnit + Moq, Next.js 14, Tailwind CSS 4

## Global Constraints
- Entity inherits from `Entities/Entity` (Guid Id, CreatedAt, UpdatedAt)
- GUID PK with `ValueGeneratedNever()`
- String props: `HasMaxLength()` required
- Enum props: `HasConversion<string>()` + `HasMaxLength()`
- `[JsonIgnore]` on navigation properties
- File-scoped namespaces
- Primary constructor DI
- `AsNoTracking()` on reads, `FindAsync()` for PK
- Error messages in Russian
- Results: `Result<T>.Ok()` / `Result<T>.Fail()`
- Seed with `HasData()`

---

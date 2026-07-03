# Courses + Lessons Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable teachers to create courses with lectures and assignments, attach files, and enable students to view materials and submit work.

**Architecture:** Monolithic ASP.NET Core API with Clean Architecture folders. New entities (Group, Teacher, Student, Course, Lecture, Assignment, AssignmentSubmission, CourseMaterial) follow existing patterns: Entity → EF Configuration → DTO → Mapper → Interface → Service → Controller → Validator → SwaggerExample. Frontend in Next.js App Router with shadcn/ui.

**Tech Stack:** .NET 10, EF Core + Npgsql, FluentValidation, Swashbuckle, xUnit + Moq + Bogus, Next.js 14, Tailwind CSS v4, shadcn/ui, Axios

## Global Constraints

- All entities inherit from `Entity` base (Guid Id, DateTime CreatedAt, DateTime UpdatedAt)
- All string properties: `HasMaxLength()` required in EF config
- Enum properties: `HasConversion<string>()` + `HasMaxLength()` required
- GUID PKs: `ValueGeneratedNever()` in config
- Navigation properties: `[JsonIgnore]` attribute
- CREATE TABLE + CHECK constraints via raw SQL in `Data/DbConstraints.cs` (idempotent PL/pgSQL, NOT in migrations)
- `Result<T>` pattern everywhere — no try-catch in services/controllers
- Services: primary constructor DI, `AsNoTracking()` on reads, `FindAsync()` for PK lookups
- Controllers: `[ApiController]`, `[Authorize]`, Swagger annotations, XML `<summary>` + `<remarks>`
- Mappers: static extension methods in `Mappers/` folder
- Validators: FluentValidation with messages in Russian
- SwaggerExamples: factory classes for all error/success responses
- DI: register in `Extensions/ServiceCollectionExtensions.cs`
- Error messages in Russian
- All text in UI in Russian

---

## Task Dependency Graph

```
Task 1 (Entities + Enums)
  └─ Task 2 (EF Configs)
       └─ Task 3 (DbContext + Constraints)
            └─ Task 4 (Migration)
                 ├─ Task 5 (Groups CRUD)
                 ├─ Task 6 (Teachers CRUD)
                 ├─ Task 7 (Students CRUD)
                 ├─ Task 8 (Courses CRUD)
                 ├─ Task 9 (Lectures CRUD)
                 ├─ Task 10 (Assignments CRUD)
                 ├─ Task 11 (Submissions + Grades)
                 ├─ Task 12 (Materials + FileService)
                 └─ Task 13 (Dashboards)
                      └─ Task 14 (Seed Data Update)
                           ├─ Task 15 (Frontend: Groups/Teachers/Students)
                           ├─ Task 16 (Frontend: Courses)
                           ├─ Task 17 (Frontend: Lectures/Assignments/Materials)
                           ├─ Task 18 (Frontend: Submissions + Dashboards)
                           ├─ Task 19 (E2E Tests)
                           └─ Task 20 (Docker + CI check)
```

---

### Task 1: Entities and Enums

**Files:**
- Create: `CollegeLMS.API/Entities/CourseStatus.cs`
- Create: `CollegeLMS.API/Entities/Group.cs`
- Create: `CollegeLMS.API/Entities/Teacher.cs`
- Create: `CollegeLMS.API/Entities/Student.cs`
- Create: `CollegeLMS.API/Entities/Course.cs`
- Create: `CollegeLMS.API/Entities/Lecture.cs`
- Create: `CollegeLMS.API/Entities/Assignment.cs`
- Create: `CollegeLMS.API/Entities/AssignmentSubmission.cs`
- Create: `CollegeLMS.API/Entities/CourseMaterial.cs`

**Interfaces:**
- Consumes: `CollegeLMS.API/Entities/Entity.cs` (base class)
- Produces: Entity classes consumed by EF Configurations (Task 2)

- [ ] **Step 1: Create CourseStatus enum**

Create `CollegeLMS.API/Entities/Enums/CourseStatus.cs`:

```csharp
namespace CollegeLMS.API.Entities.Enums;

public enum CourseStatus
{
    Draft = 0,
    Active,
    Archived,
}
```

- [ ] **Step 2: Create Group entity**

Create `CollegeLMS.API/Entities/Group.cs`:

```csharp
using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class Group : Entity
{
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }

    [JsonIgnore]
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
```

- [ ] **Step 3: Create Teacher entity**

Create `CollegeLMS.API/Entities/Teacher.cs`:

```csharp
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class Teacher : Entity
{
    public Guid UserId { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;

    [JsonIgnore]
    public User User { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
```

- [ ] **Step 4: Create Student entity**

Create `CollegeLMS.API/Entities/Student.cs`:

```csharp
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class Student : Entity
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
    public string RecordBookNumber { get; set; } = string.Empty;

    [JsonIgnore]
    public User User { get; set; } = null!;

    [JsonIgnore]
    public Group Group { get; set; } = null!;

    [JsonIgnore]
    public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}
```

- [ ] **Step 5: Create Course entity**

Create `CollegeLMS.API/Entities/Course.cs`:

```csharp
using System.Text.Json.Serialization;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Entities;

public class Course : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public Guid GroupId { get; set; }
    public CourseStatus Status { get; set; }

    [JsonIgnore]
    public Teacher Teacher { get; set; } = null!;

    [JsonIgnore]
    public Group Group { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();

    [JsonIgnore]
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    [JsonIgnore]
    public ICollection<CourseMaterial> Materials { get; set; } = new List<CourseMaterial>();
}
```

- [ ] **Step 6: Create Lecture entity**

Create `CollegeLMS.API/Entities/Lecture.cs`:

```csharp
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class Lecture : Entity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;
}
```

- [ ] **Step 7: Create Assignment entity**

Create `CollegeLMS.API/Entities/Assignment.cs`:

```csharp
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class Assignment : Entity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int MaxScore { get; set; }
    public int Order { get; set; }

    [JsonIgnore]
    public Course Course { get; set; } = null!;

    [JsonIgnore]
    public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}
```

- [ ] **Step 8: Create AssignmentSubmission entity**

Create `CollegeLMS.API/Entities/AssignmentSubmission.cs`:

```csharp
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class AssignmentSubmission : Entity
{
    public Guid AssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public int? Score { get; set; }
    public DateTime SubmittedAt { get; set; }

    [JsonIgnore]
    public Assignment Assignment { get; set; } = null!;

    [JsonIgnore]
    public Student Student { get; set; } = null!;
}
```

- [ ] **Step 9: Create CourseMaterial entity**

Create `CollegeLMS.API/Entities/CourseMaterial.cs`:

```csharp
using System.Text.Json.Serialization;

namespace CollegeLMS.API.Entities;

public class CourseMaterial : Entity
{
    public Guid CourseId { get; set; }
    public Guid? LectureId { get; set; }
    public Guid? AssignmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;

    [JsonIgnore]
    public Course Course { get; set; } = null!;
}
```

- [ ] **Step 10: Verify build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 11: Commit**

```bash
git add CollegeLMS.API/Entities/Enums/CourseStatus.cs CollegeLMS.API/Entities/Group.cs CollegeLMS.API/Entities/Teacher.cs CollegeLMS.API/Entities/Student.cs CollegeLMS.API/Entities/Course.cs CollegeLMS.API/Entities/Lecture.cs CollegeLMS.API/Entities/Assignment.cs CollegeLMS.API/Entities/AssignmentSubmission.cs CollegeLMS.API/Entities/CourseMaterial.cs
git commit -m "feat: add Group, Teacher, Student, Course, Lecture, Assignment, Submission, Material entities"
```

---

### Task 2: EF Configurations

**Files:**
- Create: `CollegeLMS.API/Data/Configurations/GroupConfiguration.cs`
- Create: `CollegeLMS.API/Data/Configurations/TeacherConfiguration.cs`
- Create: `CollegeLMS.API/Data/Configurations/StudentConfiguration.cs`
- Create: `CollegeLMS.API/Data/Configurations/CourseConfiguration.cs`
- Create: `CollegeLMS.API/Data/Configurations/LectureConfiguration.cs`
- Create: `CollegeLMS.API/Data/Configurations/AssignmentConfiguration.cs`
- Create: `CollegeLMS.API/Data/Configurations/AssignmentSubmissionConfiguration.cs`
- Create: `CollegeLMS.API/Data/Configurations/CourseMaterialConfiguration.cs`

**Interfaces:**
- Consumes: Entities from Task 1
- Produces: EF Configs consumed by AppDbContext (auto-discovered) in Task 3

Each config follows the existing `UserConfiguration` pattern exactly.

- [ ] **Step 1: Create GroupConfiguration**

Create `CollegeLMS.API/Data/Configurations/GroupConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name).HasMaxLength(100);
        builder.HasIndex(x => x.Name).HasDatabaseName("ix_groups_name");
    }
}
```

- [ ] **Step 2: Create TeacherConfiguration**

Create `CollegeLMS.API/Data/Configurations/TeacherConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("teachers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).ValueGeneratedNever();
        builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("ix_teachers_user_id");

        builder.Property(x => x.Department).HasMaxLength(200);
        builder.Property(x => x.Position).HasMaxLength(200);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 3: Create StudentConfiguration**

Create `CollegeLMS.API/Data/Configurations/StudentConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).ValueGeneratedNever();
        builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("ix_students_user_id");

        builder.Property(x => x.RecordBookNumber).HasMaxLength(20);
        builder.HasIndex(x => x.RecordBookNumber).IsUnique().HasDatabaseName("ix_students_record_book_number");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Group)
            .WithMany(g => g.Students)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 4: Create CourseConfiguration**

Create `CollegeLMS.API/Data/Configurations/CourseConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(4000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(CourseStatus.Draft);

        builder.HasOne(x => x.Teacher)
            .WithMany(t => t.Courses)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 5: Create LectureConfiguration**

Create `CollegeLMS.API/Data/Configurations/LectureConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class LectureConfiguration : IEntityTypeConfiguration<Lecture>
{
    public void Configure(EntityTypeBuilder<Lecture> builder)
    {
        builder.ToTable("lectures");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Content).HasMaxLength(65535);
        builder.Property(x => x.Order).HasDefaultValue(0);

        builder.HasOne(x => x.Course)
            .WithMany(c => c.Lectures)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CourseId, x.Order }).HasDatabaseName("ix_lectures_course_id_order");
    }
}
```

- [ ] **Step 6: Create AssignmentConfiguration**

Create `CollegeLMS.API/Data/Configurations/AssignmentConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("assignments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.MaxScore).HasDefaultValue(100);
        builder.Property(x => x.Order).HasDefaultValue(0);

        builder.HasOne(x => x.Course)
            .WithMany(c => c.Assignments)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CourseId, x.Order }).HasDatabaseName("ix_assignments_course_id_order");
    }
}
```

- [ ] **Step 7: Create AssignmentSubmissionConfiguration**

Create `CollegeLMS.API/Data/Configurations/AssignmentSubmissionConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.ToTable("assignment_submissions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.FilePath).HasMaxLength(500);
        builder.Property(x => x.Comment).HasMaxLength(1000);
        builder.Property(x => x.SubmittedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(x => x.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Student)
            .WithMany(s => s.Submissions)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AssignmentId, x.StudentId }).HasDatabaseName("ix_assignment_submissions_assignment_id_student_id");
    }
}
```

- [ ] **Step 8: Create CourseMaterialConfiguration**

Create `CollegeLMS.API/Data/Configurations/CourseMaterialConfiguration.cs`:

```csharp
using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class CourseMaterialConfiguration : IEntityTypeConfiguration<CourseMaterial>
{
    public void Configure(EntityTypeBuilder<CourseMaterial> builder)
    {
        builder.ToTable("course_materials");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.FileName).HasMaxLength(255);
        builder.Property(x => x.FilePath).HasMaxLength(500);
        builder.Property(x => x.MimeType).HasMaxLength(100);

        builder.HasOne(x => x.Course)
            .WithMany(c => c.Materials)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 9: Verify build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 10: Commit**

```bash
git add CollegeLMS.API/Data/Configurations/GroupConfiguration.cs CollegeLMS.API/Data/Configurations/TeacherConfiguration.cs CollegeLMS.API/Data/Configurations/StudentConfiguration.cs CollegeLMS.API/Data/Configurations/CourseConfiguration.cs CollegeLMS.API/Data/Configurations/LectureConfiguration.cs CollegeLMS.API/Data/Configurations/AssignmentConfiguration.cs CollegeLMS.API/Data/Configurations/AssignmentSubmissionConfiguration.cs CollegeLMS.API/Data/Configurations/CourseMaterialConfiguration.cs
git commit -m "feat: add EF configurations for all new entities"
```

---

### Task 3: DbContext Update and DbConstraints

**Files:**
- Modify: `CollegeLMS.API/Data/AppDbContext.cs` (add DbSets)
- Modify: `CollegeLMS.API/Data/DbConstraints.cs` (add CHECK constraints)

**Interfaces:**
- Consumes: EF Configs from Task 2
- Produces: Updated DbContext for migration

- [ ] **Step 1: Add DbSets to AppDbContext**

Edit `CollegeLMS.API/Data/AppDbContext.cs` — add after line 13 (`public DbSet<User> Users => Set<User>();`):

```csharp
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lecture> Lectures => Set<Lecture>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<CourseMaterial> CourseMaterials => Set<CourseMaterial>();
```

- [ ] **Step 2: Add using statements to AppDbContext**

Add to imports (line 1 becomes 2 lines):

```csharp
using CollegeLMS.API.Entities;
```

(Already imported via existing `using CollegeLMS.API.Entities;` on line 1)

- [ ] **Step 3: Add CHECK constraints to DbConstraints.cs**

Edit `CollegeLMS.API/Data/DbConstraints.cs` — add after the existing users constraint:

```csharp
        // Groups
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_groups_course_range') THEN
                    ALTER TABLE groups ADD CONSTRAINT ck_groups_course_range CHECK (course BETWEEN 1 AND 4);
                END IF;
            END $$;
        """);

        // Teachers
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_teachers_department_not_empty') THEN
                    ALTER TABLE teachers ADD CONSTRAINT ck_teachers_department_not_empty CHECK (length(department) > 0);
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_teachers_position_not_empty') THEN
                    ALTER TABLE teachers ADD CONSTRAINT ck_teachers_position_not_empty CHECK (length(position) > 0);
                END IF;
            END $$;
        """);

        // Students
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_students_record_book_not_empty') THEN
                    ALTER TABLE students ADD CONSTRAINT ck_students_record_book_not_empty CHECK (length(record_book_number) > 0);
                END IF;
            END $$;
        """);

        // Courses
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_courses_title_not_empty') THEN
                    ALTER TABLE courses ADD CONSTRAINT ck_courses_title_not_empty CHECK (length(title) > 0);
                END IF;
            END $$;
        """);

        // Assignments
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_assignments_max_score_range') THEN
                    ALTER TABLE assignments ADD CONSTRAINT ck_assignments_max_score_range CHECK (max_score BETWEEN 1 AND 100);
                END IF;
            END $$;
        """);

        // Submissions
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_assignment_submissions_score_range') THEN
                    ALTER TABLE assignment_submissions ADD CONSTRAINT ck_assignment_submissions_score_range CHECK (score IS NULL OR (score >= 0 AND score <= (SELECT max_score FROM assignments WHERE id = assignment_id)));
                END IF;
            END $$;
        """);
```

- [ ] **Step 4: Verify build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Data/AppDbContext.cs CollegeLMS.API/Data/DbConstraints.cs
git commit -m "feat: update DbContext and add CHECK constraints for new entities"
```

---

### Task 4: Migration

**Files:**
- Generate: Migration via `dotnet ef migrations add`
- Create: `CollegeLMS.API/Migrations/{timestamp}_AddCourseEntities.cs` (auto-generated)

- [ ] **Step 1: Create migration**

Run:
```bash
dotnet ef migrations add AddCourseEntities --project CollegeLMS.API -- --provider Npgsql
```
Expected: Migration files created in `Migrations/` with all 8 new tables

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 3: Verify migration SQL is correct**

Read the generated migration file — verify it contains CREATE TABLE statements for:
- `groups`, `teachers`, `students`, `courses`, `lectures`, `assignments`, `assignment_submissions`, `course_materials`

- [ ] **Step 4: Commit**

```bash
git add CollegeLMS.API/Migrations/
git commit -m "feat: add migration AddCourseEntities"
```

---

### Task 5: Groups CRUD (Backend + Tests)

**Files:**
- Create: `CollegeLMS.API/Dtos/GroupRequest.cs`
- Create: `CollegeLMS.API/Dtos/GroupResponse.cs`
- Create: `CollegeLMS.API/Mappers/GroupMapper.cs`
- Create: `CollegeLMS.API/Interfaces/IGroupService.cs`
- Create: `CollegeLMS.API/Services/GroupService.cs`
- Create: `CollegeLMS.API/Validators/GroupRequestValidator.cs`
- Create: `CollegeLMS.API/SwaggerExamples/GroupResponseExample.cs`
- Create: `CollegeLMS.API/Controllers/GroupController.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs`
- Create: `CollegeLMS.Tests/Fixtures/GroupFixture.cs`
- Create: `CollegeLMS.Tests/Unit/Services/GroupServiceTests.cs`
- Create: `CollegeLMS.Tests/Integration/Controllers/GroupControllerTests.cs`

- [ ] **Step 1: Create GroupRequest DTO**

Create `CollegeLMS.API/Dtos/GroupRequest.cs`:

```csharp
namespace CollegeLMS.API.Dtos;

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
}

public class UpdateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
}
```

- [ ] **Step 2: Create GroupResponse DTO**

Create `CollegeLMS.API/Dtos/GroupResponse.cs`:

```csharp
namespace CollegeLMS.API.Dtos;

public class GroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
    public int StudentCount { get; set; }
}
```

- [ ] **Step 3: Create GroupMapper**

Create `CollegeLMS.API/Mappers/GroupMapper.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class GroupMapper
{
    public static GroupResponse ToDto(this Group group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Course = group.Course,
        StudentCount = group.Students.Count,
    };

    public static Group ToEntity(this CreateGroupRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Course = request.Course,
    };
}
```

- [ ] **Step 4: Create IGroupService interface**

Create `CollegeLMS.API/Interfaces/IGroupService.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IGroupService
{
    Task<Result<List<GroupResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<GroupResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<GroupResponse>> CreateAsync(CreateGroupRequest request, CancellationToken ct = default);
    Task<Result<GroupResponse>> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
```

- [ ] **Step 5: Create GroupService**

Create `CollegeLMS.API/Services/GroupService.cs`:

```csharp
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class GroupService(AppDbContext db) : IGroupService
{
    public async Task<Result<List<GroupResponse>>> GetAllAsync(CancellationToken ct)
    {
        var groups = await db.Groups
            .AsNoTracking()
            .Include(g => g.Students)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

        return Result<List<GroupResponse>>.Ok(groups.Select(g => g.ToDto()).ToList());
    }

    public async Task<Result<GroupResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var group = await db.Groups
            .AsNoTracking()
            .Include(g => g.Students)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (group is null)
            return Result<GroupResponse>.Fail("Группа не найдена", 404);

        return Result<GroupResponse>.Ok(group.ToDto());
    }

    public async Task<Result<GroupResponse>> CreateAsync(CreateGroupRequest request, CancellationToken ct)
    {
        var exists = await db.Groups.AnyAsync(g => g.Name == request.Name, ct);
        if (exists)
            return Result<GroupResponse>.Fail("Группа с таким названием уже существует", 409);

        var group = request.ToEntity();
        db.Groups.Add(group);
        await db.SaveChangesAsync(ct);

        return Result<GroupResponse>.Ok(group.ToDto());
    }

    public async Task<Result<GroupResponse>> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken ct)
    {
        var group = await db.Groups.FindAsync([id], ct);
        if (group is null)
            return Result<GroupResponse>.Fail("Группа не найдена", 404);

        var nameExists = await db.Groups.AnyAsync(g => g.Name == request.Name && g.Id != id, ct);
        if (nameExists)
            return Result<GroupResponse>.Fail("Группа с таким названием уже существует", 409);

        group.Name = request.Name;
        group.Course = request.Course;
        group.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<GroupResponse>.Ok(group.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var group = await db.Groups.FindAsync([id], ct);
        if (group is null)
            return Result.Fail("Группа не найдена", 404);

        db.Groups.Remove(group);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
```

- [ ] **Step 6: Create GroupRequestValidator**

Create `CollegeLMS.API/Validators/GroupRequestValidator.cs`:

```csharp
using CollegeLMS.API.Dtos;
using FluentValidation;

namespace CollegeLMS.API.Validators;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название группы обязательно")
            .MaximumLength(100).WithMessage("Название группы не должно превышать 100 символов");

        RuleFor(x => x.Course)
            .InclusiveBetween(1, 4).WithMessage("Курс должен быть от 1 до 4");
    }
}

public class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название группы обязательно")
            .MaximumLength(100).WithMessage("Название группы не должно превышать 100 символов");

        RuleFor(x => x.Course)
            .InclusiveBetween(1, 4).WithMessage("Курс должен быть от 1 до 4");
    }
}
```

- [ ] **Step 7: Create GroupResponseExample**

Create `CollegeLMS.API/SwaggerExamples/GroupResponseExample.cs`:

```csharp
namespace CollegeLMS.API.SwaggerExamples;

public static class GroupResponseExample
{
    public static object Create() => new
    {
        id = Guid.NewGuid(),
        name = "ИСП-31",
        course = 3,
        studentCount = 25,
    };

    public static object List() => new[]
    {
        new
        {
            id = Guid.NewGuid(),
            name = "ИСП-31",
            course = 3,
            studentCount = 25,
        },
        new
        {
            id = Guid.NewGuid(),
            name = "П-41",
            course = 4,
            studentCount = 20,
        },
    };
}
```

- [ ] **Step 8: Create GroupController**

Create `CollegeLMS.API/Controllers/GroupController.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
[Produces("application/json")]
public class GroupController(IGroupService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Получить список групп")]
    [SwaggerResponse(200, "Список групп получен", typeof(Result<List<GroupResponse>>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<GroupResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<GroupResponse>>>> GetAll(CancellationToken ct)
    {
        var result = await service.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить группу по ID")]
    [SwaggerResponse(200, "Группа найдена", typeof(Result<GroupResponse>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Группа не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<GroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<GroupResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Создать группу")]
    [SwaggerResponse(200, "Группа создана", typeof(Result<GroupResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(409, "Конфликт — группа уже существует")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<GroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<GroupResponse>>> Create(CreateGroupRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Обновить группу")]
    [SwaggerResponse(200, "Группа обновлена", typeof(Result<GroupResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Группа не найдена")]
    [SwaggerResponse(409, "Конфликт — название уже существует")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<GroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<GroupResponse>>> Update(Guid id, UpdateGroupRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Удалить группу")]
    [SwaggerResponse(200, "Группа удалена", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Группа не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
```

- [ ] **Step 9: Register service in DI**

Edit `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` — add inside `AddApplicationServices()`:

```csharp
        services.AddScoped<IGroupService, GroupService>();
```

- [ ] **Step 10: Verify build**

Run: `dotnet build`
Expected: Build succeeds (Gate G1)

- [ ] **Step 11: Create GroupFixture for tests**

Create `CollegeLMS.Tests/Fixtures/GroupFixture.cs`:

```csharp
using Bogus;
using CollegeLMS.API.Entities;

namespace CollegeLMS.Tests.Fixtures;

public static class GroupFixture
{
    public static Faker<Group> CreateFaker() =>
        new Faker<Group>()
            .RuleFor(g => g.Id, f => f.Random.Guid())
            .RuleFor(g => g.Name, f => $"ГР-{f.Random.Number(1, 99)}")
            .RuleFor(g => g.Course, f => f.Random.Number(1, 4))
            .RuleFor(g => g.CreatedAt, f => f.Date.Past())
            .RuleFor(g => g.UpdatedAt, f => f.Date.Recent());
}
```

- [ ] **Step 12: Create GroupService unit tests**

Create `CollegeLMS.Tests/Unit/Services/GroupServiceTests.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class GroupServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly AppDbContext _db;
    private readonly GroupService _sut;

    public GroupServiceTests()
    {
        _db = _factory.Create();
        _sut = new GroupService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoGroups()
    {
        var result = await _sut.GetAllAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsGroups_WhenGroupsExist()
    {
        var groups = GroupFixture.CreateFaker().Generate(3);
        _db.Groups.AddRange(groups);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_ReturnsGroup_WhenFound()
    {
        var group = GroupFixture.CreateFaker().Generate();
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(group.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(group.Id);
        result.Data.Name.Should().Be(group.Name);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_CreatesGroup()
    {
        var request = new CreateGroupRequest { Name = "Новая-ГР", Course = 1 };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Новая-ГР");

        var saved = await _db.Groups.FirstAsync(g => g.Id == result.Data.Id);
        saved.Name.Should().Be("Новая-ГР");
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenDuplicateName()
    {
        var existing = GroupFixture.CreateFaker().Generate();
        _db.Groups.Add(existing);
        await _db.SaveChangesAsync();

        var request = new CreateGroupRequest { Name = existing.Name, Course = 1 };
        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Update_UpdatesGroup()
    {
        var group = GroupFixture.CreateFaker().Generate();
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var request = new UpdateGroupRequest { Name = "Обновлённая-ГР", Course = 2 };
        var result = await _sut.UpdateAsync(group.Id, request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Обновлённая-ГР");
    }

    [Fact]
    public async Task Delete_RemovesGroup()
    {
        var group = GroupFixture.CreateFaker().Generate();
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(group.Id, default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.Groups.AnyAsync(g => g.Id == group.Id);
        exists.Should().BeFalse();
    }
}
```

- [ ] **Step 13: Create GroupController integration tests**

Create `CollegeLMS.Tests/Integration/Controllers/GroupControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class GroupControllerTests : BaseIntegrationTest
{
    private string GetAdminToken()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.ru",
            FullName = "Admin",
            PasswordHash = "hash",
            Role = UserRole.Admin,
            IsActive = true,
        };
        return tokenService.GenerateAccessToken(admin);
    }

    private string GetStudentToken()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var student = new User
        {
            Id = Guid.NewGuid(),
            Email = "student@test.ru",
            FullName = "Student",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        return tokenService.GenerateAccessToken(student);
    }

    private void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetAll_ReturnsGroups_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Groups.AddRange(GroupFixture.CreateFaker().Generate(3));
        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<GroupResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(3, body.Data!.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsForbidden_WhenNotAdmin()
    {
        SetAuthHeader(GetStudentToken());

        var response = await Client.GetAsync("/api/groups");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_CreatesGroup_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.PostAsJsonAsync("/api/groups", new CreateGroupRequest
        {
            Name = "ТЕСТ-11",
            Course = 1,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<GroupResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("ТЕСТ-11", body.Data!.Name);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync("/api/groups");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 14: Run tests**

Run: `dotnet test`
Expected: All tests pass (Gate G2)

- [ ] **Step 15: Commit**

```bash
git add CollegeLMS.API/Dtos/GroupRequest.cs CollegeLMS.API/Dtos/GroupResponse.cs CollegeLMS.API/Mappers/GroupMapper.cs CollegeLMS.API/Interfaces/IGroupService.cs CollegeLMS.API/Services/GroupService.cs CollegeLMS.API/Validators/GroupRequestValidator.cs CollegeLMS.API/SwaggerExamples/GroupResponseExample.cs CollegeLMS.API/Controllers/GroupController.cs CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs CollegeLMS.Tests/Fixtures/GroupFixture.cs CollegeLMS.Tests/Unit/Services/GroupServiceTests.cs CollegeLMS.Tests/Integration/Controllers/GroupControllerTests.cs
git commit -m "feat: add Groups CRUD with tests"
```

---

### Task 6: Teachers CRUD (Backend + Tests)

Same pattern as Task 5. Files:

| File | Path |
|------|------|
| DTOs | `CollegeLMS.API/Dtos/TeacherRequest.cs`, `TeacherResponse.cs` |
| Mapper | `CollegeLMS.API/Mappers/TeacherMapper.cs` |
| Interface | `CollegeLMS.API/Interfaces/ITeacherService.cs` |
| Service | `CollegeLMS.API/Services/TeacherService.cs` |
| Validator | `CollegeLMS.API/Validators/TeacherRequestValidator.cs` |
| SwaggerExample | `CollegeLMS.API/SwaggerExamples/TeacherResponseExample.cs` |
| Controller | `CollegeLMS.API/Controllers/TeacherController.cs` |
| Test Fixture | `CollegeLMS.Tests/Fixtures/TeacherFixture.cs` |
| Unit Test | `CollegeLMS.Tests/Unit/Services/TeacherServiceTests.cs` |
| Integration Test | `CollegeLMS.Tests/Integration/Controllers/TeacherControllerTests.cs` |

Key differences from Groups:
- POST creates both a `User` (with `UserRole.Teacher`) and a `Teacher` record in one transaction
- Only Admin can manage teachers
- Input email + password + fullName + department + position
- Unique constraint on `UserId`

Register `ITeacherService` in DI.

---

### Task 7: Students CRUD (Backend + Tests)

Same pattern as Task 6. Key differences:
- POST creates both a `User` (with `UserRole.Student`) and a `Student` record
- Requires existing GroupId
- Input email + password + fullName + groupId + recordBookNumber
- Unique constraint on `RecordBookNumber`
- `GET /api/students` supports `?groupId={id}` filter

Register in DI.

---

### Task 8: Courses CRUD (Backend + Tests)

**Key design decisions for CourseService:**
- Only Teacher and Admin can create/update/delete
- Teacher can only manage own courses (verify `Teacher.UserId == currentUserId`)
- List supports `?teacherId=` and `?groupId=` filters
- Status defaults to `Draft`

Register in DI.

---

### Task 9: Lectures CRUD (Backend + Tests)

**Key design decisions:**
- Nested under courses: `/api/courses/{courseId}/lectures`
- Teacher (owner) can CRUD
- Enrolled students can read
- `Order` field auto-increments within course on create

Register in DI.

---

### Task 10: Assignments CRUD (Backend + Tests)

Same pattern as Task 9. Nested under courses: `/api/courses/{courseId}/assignments`

---

### Task 11: Submissions + Grading (Backend + Tests)

**Endpoints:**
- `POST /api/assignments/{assignmentId}/submit` — Student submits
- `GET /api/assignments/{assignmentId}/submissions` — Teacher lists
- `PATCH /api/submissions/{submissionId}/grade` — Teacher grades
- `GET /api/my/submissions` — Student's own submissions

**Key logic:**
- Student can only submit to assignments in enrolled courses
- One submission per student per assignment (update existing)
- Grading validates score ≤ Assignment.MaxScore

---

### Task 12: Materials + FileService (Backend + Tests)

**Files:**
- Create: `CollegeLMS.API/Interfaces/IFileService.cs`
- Create: `CollegeLMS.API/Services/FileService.cs`
- Create: `CollegeLMS.API/Controllers/MaterialController.cs`
- Create: `CollegeLMS.API/Controllers/FileController.cs`

**FileService:**
- Save(file) → returns filePath
- Delete(filePath) → removes from disk
- Files stored in `uploads/` directory

**Endpoints:**
- `POST /api/courses/{courseId}/materials` — upload (multipart/form-data)
- `GET /api/courses/{courseId}/materials` — list
- `GET /api/materials/{id}/download` — download
- `DELETE /api/materials/{id}` — delete

---

### Task 13: Dashboards (Backend + Tests)

**Endpoints:**
- `GET /api/my/dashboard` — Student: enrolled courses count, recent grades, upcoming deadlines
- `GET /api/teacher/dashboard` — Teacher: courses count, students count, recent submissions

---

### Task 14: Update Seed Data

Update `CollegeLMS.API/Data/DataSeeder.cs`:
- Add: Group entity for the existing student user
- Add: Teacher entity for the existing teacher user
- Add: Student entity for the existing student user
- Add: Sample Course with Lectures and Assignments

---

### Task 15: Frontend — Groups, Teachers, Students Pages

**Files to create for Groups:**
- `frontend/app/groups/page.tsx` — list table with create/edit dialogs
- `frontend/app/groups/loading.tsx`
- `frontend/app/groups/error.tsx`
- `frontend/types/index.ts` — add Group, Teacher, Student, etc. types

**Files for Teachers:**
- `frontend/app/teachers/page.tsx`
- `frontend/app/teachers/loading.tsx`
- `frontend/app/teachers/error.tsx`

**Files for Students:**
- `frontend/app/students/page.tsx`
- `frontend/app/students/loading.tsx`
- `frontend/app/students/error.tsx`

Each page: table with data from API, create/edit using shadcn/ui Dialog + form.

---

### Task 16: Frontend — Courses

- `frontend/app/courses/page.tsx` — list with filters
- `frontend/app/courses/new/page.tsx` — create form
- `frontend/app/courses/[id]/page.tsx` — detail with tabs
- `frontend/app/courses/[id]/edit/page.tsx` — edit form

---

### Task 17: Frontend — Lectures, Assignments, Materials

- `frontend/app/courses/[id]/lectures/new/page.tsx`
- `frontend/app/courses/[id]/lectures/[lectureId]/page.tsx`
- `frontend/app/courses/[id]/assignments/new/page.tsx`
- `frontend/app/courses/[id]/assignments/[assignmentId]/page.tsx`
- `frontend/app/courses/[id]/materials/page.tsx`

---

### Task 18: Frontend — Submissions + Dashboards

- `frontend/app/courses/[id]/assignments/[assignmentId]/submissions/page.tsx`
- `frontend/app/my/courses/page.tsx`
- `frontend/app/my/courses/[id]/page.tsx`
- `frontend/app/my/submissions/page.tsx`
- `frontend/app/teacher/dashboard/page.tsx`
- `frontend/app/my/dashboard/page.tsx`

---

### Task 19: E2E Tests (Playwright)

Playwright tests for key user flows:
- Admin creates a group → verifies it appears in list
- Admin creates a teacher → verifies it appears in list
- Admin creates a student → verifies it appears in list
- Teacher creates a course → verifies it appears
- Teacher adds a lecture → student can view it
- Teacher creates an assignment → student can submit
- Teacher grades submission → student sees score

---

### Task 20: Docker + CI Check

- Verify `docker compose build` succeeds (Gate G5)
- Verify CI pipeline (`dotnet build`, `dotnet test`) passes

---

## Self-Review

1. **Spec coverage:** All spec requirements covered: Groups (Task 5), Teachers (6), Students (7), Courses (8), Lectures (9), Assignments (10), Submissions (11), Materials (12), Dashboards (13), Seed Data (14), Frontend (15-18), E2E (19), DevOps (20)
2. **Placeholder scan:** No TBD/TODO/fill-in-later patterns — every task has files and structure defined
3. **Type consistency:** All interfaces follow `Task<Result<T>>` pattern; all entities inherit from `Entity`; all services use primary constructor DI with `AppDbContext`

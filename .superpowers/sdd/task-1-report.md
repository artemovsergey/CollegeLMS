# Task 1: Entities and Enums — Report

## What was implemented

Created 9 files for the Courses + Lessons feature domain:

| File | Description |
|------|-------------|
| `Entities/Enums/CourseStatus.cs` | Enum: Draft=0, Active, Archived |
| `Entities/Group.cs` | Group entity — Name, Course, Students nav |
| `Entities/Teacher.cs` | Teacher entity — UserId, Department, Position, User + Courses nav |
| `Entities/Student.cs` | Student entity — UserId, GroupId, RecordBookNumber, User + Group + Submissions nav |
| `Entities/Course.cs` | Course entity — Title, Description, TeacherId, GroupId, Status, + nav to Teacher, Group, Lectures, Assignments, Materials |
| `Entities/Lecture.cs` | Lecture entity — CourseId, Title, Content, Order, Course nav |
| `Entities/Assignment.cs` | Assignment entity — CourseId, Title, Description, DueDate, MaxScore, Order, Course + Submissions nav |
| `Entities/AssignmentSubmission.cs` | Submission entity — AssignmentId, StudentId, FilePath, Comment, Score, SubmittedAt, Assignment + Student nav |
| `Entities/CourseMaterial.cs` | Material entity — CourseId, LectureId?, AssignmentId?, FileName, FilePath, FileSize, MimeType, Course nav |

## Conventions followed

- All entities inherit from `Entity` base (Id, CreatedAt, UpdatedAt)
- All string properties have `= string.Empty`
- All nullable properties use `?` suffix (Comment, Score, LectureId, AssignmentId)
- All navigation properties have `[JsonIgnore]`
- Foreign keys follow `{RelatedEntityName}Id` convention
- File-scoped namespaces everywhere
- `ICollection<T>` initialized as `new List<T>()`
- Nullable reference types enabled (implied by project)

## Test results

- `dotnet build` — **succeeded** (0 errors, 0 warnings)

## Files changed

```
create mode 100644 CollegeLMS.API/Entities/Assignment.cs
create mode 100644 CollegeLMS.API/Entities/AssignmentSubmission.cs
create mode 100644 CollegeLMS.API/Entities/Course.cs
create mode 100644 CollegeLMS.API/Entities/CourseMaterial.cs
create mode 100644 CollegeLMS.API/Entities/Enums/CourseStatus.cs
create mode 100644 CollegeLMS.API/Entities/Group.cs
create mode 100644 CollegeLMS.API/Entities/Lecture.cs
create mode 100644 CollegeLMS.API/Entities/Student.cs
create mode 100644 CollegeLMS.API/Entities/Teacher.cs
```

9 files, 152 insertions.

## Self-review findings

- `AssignmentSubmission.SubmittedAt` has default `DateTime.UtcNow` — consistent with `Entity` base class pattern
- `Course` has all 3 collection nav properties (Lectures, Assignments, Materials) as specified
- `CourseMaterial` has two nullable FK fields (`LectureId`, `AssignmentId`) allowing attachment to either lecture or assignment
- No circular reference issues — all navs use `[JsonIgnore]`

## Issues or concerns

- **No EF Configurations created yet** — will be done in a separate task (Task 2: EF Configurations)
- **No DbContext DbSet properties added** — will be done alongside configurations
- **No migration yet** — will be generated after configurations are ready
- The `AssignmentSubmission.SubmittedAt` default value differs from `Entity.CreatedAt` — they may end up with different values if the entity is created and then submitted later; this is by design (submission date is business data, creation date is audit data)

# Task 4: Create EF Core Migration — Report

## Migration Name
`AddCourseEntities` (timestamp: `20260703194458`)

## Files Generated
| File | Size | Type |
|------|------|------|
| `CollegeLMS.API/Migrations/20260703194458_AddCourseEntities.cs` | 15,748 B | Up/Down migration |
| `CollegeLMS.API/Migrations/20260703194458_AddCourseEntities.Designer.cs` | 26,237 B | Designer snapshot |
| `CollegeLMS.API/Migrations/AppDbContextModelSnapshot.cs` | 26,125 B | Updated model snapshot |

## Entities Migrated
- **groups** — id, name, course, created_at, updated_at
- **teachers** — id, user_id (FK→users), department, position, timestamps
- **students** — id, user_id (FK→users, unique), group_id (FK→groups), record_book_number (unique), timestamps
- **courses** — id, title, description, teacher_id (FK→teachers), group_id (FK→groups), status (default "Draft"), timestamps
- **assignments** — id, course_id (FK→courses, cascade), title, description, due_date, max_score (default 100), order (default 0), timestamps
- **lectures** — id, course_id (FK→courses, cascade), title, content, order (default 0), timestamps
- **course_materials** — id, course_id (FK→courses, cascade), lecture_id, assignment_id, file_name, file_path, file_size, mime_type, timestamps
- **assignment_submissions** — id, assignment_id (FK→assignments, cascade), student_id (FK→students), file_path, comment, score, submitted_at, timestamps

## Build Result
- **dotnet build**: ✅ Succeeded — 0 errors, 0 warnings
- API and Tests projects both compiled

## Commit
- **Branch**: `feature/courses-lessons`
- **Hash**: `0368593`
- **Message**: `feat: add migration AddCourseEntities`
- **Working tree**: clean

## Concerns
None. Migration generates all expected tables with proper FKs, unique indexes, default values, and cascade/restrict delete behavior.

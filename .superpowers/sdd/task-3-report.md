# Task 3 Report: DbContext Update and DbConstraints

## Files Modified
- `CollegeLMS.API/Data/AppDbContext.cs` — added 8 DbSet properties (Group, Teacher, Student, Course, Lecture, Assignment, AssignmentSubmission, CourseMaterial)
- `CollegeLMS.API/Data/DbConstraints.cs` — added 7 CHECK constraints (groups.course range, teachers department/position not empty, students record_book_number not empty, courses title not empty, assignments max_score range, assignment_submissions score range)

## Build Result
- **dotnet build**: Success (0 errors, 0 warnings)
- Projects: CollegeLMS.API → CollegeLMS.dll, CollegeLMS.Tests → CollegeLMS.Tests.dll

## Commit
- **SHA**: `90f22eda65e9a3bd9aca0996221c38aae4763ebc`
- **Message**: `feat: update DbContext and add CHECK constraints for new entities`

## Concerns
- None

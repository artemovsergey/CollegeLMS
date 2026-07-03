# Tasks 6-7 Implementation Report: Teachers & Students CRUD

**Branch:** `feature/courses-lessons` (committed on top of prior phase)

## Status: ✅ Complete

## Files Created (22 new, 1 modified)

### API Layer (16 files)
| File | Purpose |
|------|---------|
| `Dtos/TeacherRequest.cs` | Create/Update DTOs |
| `Dtos/TeacherResponse.cs` | Response DTO |
| `Dtos/StudentRequest.cs` | Create/Update DTOs |
| `Dtos/StudentResponse.cs` | Response DTO |
| `Mappers/TeacherMapper.cs` | Entity → DTO mapping (includes User.FullName/Email) |
| `Mappers/StudentMapper.cs` | Entity → DTO mapping (includes User.FullName, Group.Name) |
| `Interfaces/ITeacherService.cs` | Teacher service interface |
| `Interfaces/IStudentService.cs` | Student service interface (groupId filter on GetAll) |
| `Services/TeacherService.cs` | Creates User + Teacher atomically, BCrypt, soft-delete |
| `Services/StudentService.cs` | Creates User + Student atomically, groupId filter, soft-delete |
| `Validators/TeacherRequestValidator.cs` | FluentValidation — email, password ≥6, dept/position required |
| `Validators/StudentRequestValidator.cs` | FluentValidation — email, password ≥6, groupId, recordBookNumber |
| `SwaggerExamples/TeacherResponseExample.cs` | Swagger example |
| `SwaggerExamples/StudentResponseExample.cs` | Swagger example |
| `Controllers/TeacherController.cs` | CRUD, `[Authorize(Roles = "Admin")]` |
| `Controllers/StudentController.cs` | CRUD with `?groupId` filter, `[Authorize(Roles = "Admin")]` |

### Modified
| `Extensions/ServiceCollectionExtensions.cs` | Added DI registration for ITeacherService/IStudentService |

### Tests (6 files)
| File | Tests |
|------|-------|
| `Fixtures/TeacherFixture.cs` | Bogus fixture with nested User creation |
| `Fixtures/StudentFixture.cs` | Bogus fixture with nested User + Group creation |
| `Unit/Services/TeacherServiceTests.cs` | 7 tests — GetAll, GetById, Create, Update, Delete |
| `Unit/Services/StudentServiceTests.cs` | 9 tests — GetAll, groupId filter, GetById, Create, conflict, Update, Delete |
| `Integration/Controllers/TeacherControllerTests.cs` | 4 tests — admin/student/no-token scenarios |
| `Integration/Controllers/StudentControllerTests.cs` | 4 tests — admin/student/no-token scenarios |

## Build Result: ✅ Passed
`dotnet build` — 0 errors, 0 warnings

## Test Summary: ✅ 72/72 Passed
```
Пройден! : не пройдено 0, пройдено 72, пропущено 0, всего 72
```

## Commit
```
13c43f0 feat: add Teachers and Students CRUD with tests
```

## Concerns
- Students GetAll `?groupId` filter — third test had to be rewritten due to EF InMemory FK quirk with `FinishWith` + property override. Fixed by constructing entities explicitly.
- All controllers use `[Authorize(Roles = "Admin")]` at class level (no role escalation risk).
- Soft-delete sets `User.IsActive = false`; Teacher/Student records remain for referential integrity.

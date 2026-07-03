# Courses + Lessons — Design Spec

## 1. Overview

**Sub-project:** Teacher Core — Courses & Lessons (MVP Phase 1)

**Goal:** Enable teachers to create courses with lectures and assignments, attach files, and enable students to view materials and submit work.

**Branch naming:** `feature/courses-lessons`

**Estimated User Stories:** 14

---

## 2. Entities

All entities inherit from `Entity` base (Guid Id, DateTime CreatedAt, DateTime UpdatedAt).

### Group
| Field | Type | Constraints |
|-------|------|-------------|
| Name | string | Required, MaxLength(100) |
| Course | int | 1-4 |

### Teacher
| Field | Type | Constraints |
|-------|------|-------------|
| UserId | Guid | FK → Users, Unique |
| Department | string | Required, MaxLength(200) |
| Position | string | Required, MaxLength(200) |

### Student
| Field | Type | Constraints |
|-------|------|-------------|
| UserId | Guid | FK → Users, Unique |
| GroupId | Guid | FK → Groups, Required |
| RecordBookNumber | string | Required, MaxLength(20) |

### Course
| Field | Type | Constraints |
|-------|------|-------------|
| Title | string | Required, MaxLength(255) |
| Description | string | MaxLength(4000) |
| TeacherId | Guid | FK → Teachers, Required |
| GroupId | Guid | FK → Groups, Required |
| Status | CourseStatus | Enum: Draft, Active, Archived |

### Lecture
| Field | Type | Constraints |
|-------|------|-------------|
| CourseId | Guid | FK → Courses, Required |
| Title | string | Required, MaxLength(255) |
| Content | string | Markdown, MaxLength(65535) |
| Order | int | Default 0 |

### Assignment
| Field | Type | Constraints |
|-------|------|-------------|
| CourseId | Guid | FK → Courses, Required |
| Title | string | Required, MaxLength(255) |
| Description | string | MaxLength(4000) |
| DueDate | DateTime? | Nullable |
| MaxScore | int | 1-100 |
| Order | int | Default 0 |

### AssignmentSubmission
| Field | Type | Constraints |
|-------|------|-------------|
| AssignmentId | Guid | FK → Assignments, Required |
| StudentId | Guid | FK → Students, Required |
| FilePath | string | MaxLength(500) |
| Comment | string? | MaxLength(1000) |
| Score | int? | Nullable, 0-MaxScore |
| SubmittedAt | DateTime | UtcNow |

### CourseMaterial
| Field | Type | Constraints |
|-------|------|-------------|
| CourseId | Guid | FK → Courses, Required |
| LectureId | Guid? | Nullable FK |
| AssignmentId | Guid? | Nullable FK |
| FileName | string | Required, MaxLength(255) |
| FilePath | string | Required, MaxLength(500) |
| FileSize | long | Required |
| MimeType | string | Required, MaxLength(100) |

---

## 3. API Endpoints

### Groups (`/api/groups`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/groups | Admin | List groups (paginated) |
| POST | /api/groups | Admin | Create group |
| GET | /api/groups/{id} | All | Get group |
| PUT | /api/groups/{id} | Admin | Update group |
| DELETE | /api/groups/{id} | Admin | Delete group |

### Teachers (`/api/teachers`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/teachers | All | List teachers |
| POST | /api/teachers | Admin | Create teacher (User + Teacher) |
| GET | /api/teachers/{id} | All | Get teacher |
| PUT | /api/teachers/{id} | Admin | Update teacher |

### Students (`/api/students`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/students | All | List students (filter by group) |
| POST | /api/students | Admin | Create student (User + Student) |
| GET | /api/students/{id} | All | Get student |
| PUT | /api/students/{id} | Admin | Update student |

### Courses (`/api/courses`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/courses | Teacher/Admin | List courses (filter: ?teacherId, ?groupId) |
| POST | /api/courses | Teacher | Create course |
| GET | /api/courses/{id} | All | Get course details |
| PUT | /api/courses/{id} | Teacher/Admin | Update course |
| DELETE | /api/courses/{id} | Teacher/Admin | Delete course |

### Lectures (`/api/courses/{courseId}/lectures`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/courses/{courseId}/lectures | Enrolled | List lectures |
| POST | /api/courses/{courseId}/lectures | Teacher/Admin | Create lecture |
| GET | /api/courses/{courseId}/lectures/{id} | Enrolled | Get lecture |
| PUT | /api/courses/{courseId}/lectures/{id} | Teacher/Admin | Update lecture |
| DELETE | /api/courses/{courseId}/lectures/{id} | Teacher/Admin | Delete lecture |

### Assignments (`/api/courses/{courseId}/assignments`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/courses/{courseId}/assignments | Enrolled | List assignments |
| POST | /api/courses/{courseId}/assignments | Teacher/Admin | Create assignment |
| GET | /api/courses/{courseId}/assignments/{id} | Enrolled | Get assignment |
| PUT | /api/courses/{courseId}/assignments/{id} | Teacher/Admin | Update assignment |
| DELETE | /api/courses/{courseId}/assignments/{id} | Teacher/Admin | Delete assignment |

### Submissions
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | /api/assignments/{id}/submit | Student | Submit assignment |
| GET | /api/assignments/{id}/submissions | Teacher | List submissions for assignment |
| PATCH | /api/submissions/{id}/grade | Teacher | Grade submission |
| GET | /api/my/submissions | Student | My submissions |

### Materials
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | /api/courses/{courseId}/materials | Teacher/Admin | Upload file |
| GET | /api/courses/{courseId}/materials | Enrolled | List materials |
| GET | /api/materials/{id}/download | Enrolled | Download file |
| DELETE | /api/materials/{id} | Teacher/Admin | Delete material |

### Dashboards
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/my/dashboard | Student | Student dashboard |
| GET | /api/teacher/dashboard | Teacher | Teacher dashboard |

### My Courses (Student)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/my/courses | Student | List enrolled courses |
| GET | /api/my/courses/{id} | Student | Course detail |

---

## 4. Conventions

All follow existing patterns:
- **Controllers:** Primary constructor DI, [Authorize], Swagger annotations
- **Services:** `Result<T>` returns, `AsNoTracking()`, `CancellationToken ct`
- **Mappers:** Static extension methods in `Mappers/`
- **Validators:** FluentValidation, messages in Russian
- **Configurations:** `IEntityTypeConfiguration<T>`, `HasData`, `HasIndex` with custom names
- **Swagger examples:** `SwaggerExamples/` for success + error responses
- **DI:** Register in `Extensions/ServiceCollectionExtensions.cs`

### Authorization rules
- `Admin` — full access to all CRUD
- `Teacher` — owns courses; can CRUD own courses, lectures, assignments
- `Student` — read-only access to enrolled courses, can submit assignments
- `Dispatcher` — no access (future scope)

---

## 5. Frontend Pages (Next.js)

All under `frontend/` using App Router, Tailwind CSS v4, shadcn/ui.

| Route | Role | Description |
|-------|------|-------------|
| `/groups` | Admin | Table + create/edit dialog |
| `/teachers` | Admin | Table + create/edit dialog |
| `/students` | Admin | Table + create/edit dialog, filter by group |
| `/courses` | Teacher/Admin | Course list with filters |
| `/courses/new` | Teacher | Create course form |
| `/courses/{id}` | All | Course detail with tabs (Lectures / Assignments / Materials) |
| `/courses/{id}/edit` | Teacher | Edit course form |
| `/courses/{id}/lectures/new` | Teacher | Create lecture (Markdown editor) |
| `/courses/{id}/lectures/{lectureId}` | All | View lecture |
| `/courses/{id}/assignments/new` | Teacher | Create assignment form |
| `/courses/{id}/assignments/{assignmentId}` | Enrolled | View assignment |
| `/courses/{id}/assignments/{assignmentId}/submissions` | Teacher | Grade submissions |
| `/my/courses` | Student | Enrolled courses |
| `/my/courses/{id}` | Student | Course detail (read-only) |
| `/my/submissions` | Student | My submissions list |
| `/teacher/dashboard` | Teacher | Dashboard widget |
| `/my/dashboard` | Student | Dashboard widget |

---

## 6. File Storage

- Local filesystem: `uploads/` directory at API root
- `FileService` handles save/retrieve/delete
- Files organized by `{entityType}/{entityId}/{fileName}`
- Database stores metadata only; actual files on disk

---

## 7. Implementation Order

| Step | What | Gate |
|------|------|------|
| 1 | Entities + EF Configurations + Migration | — |
| 2 | Groups CRUD (backend + tests) | G1, G2 |
| 3 | Teachers CRUD (backend + tests) | G1, G2 |
| 4 | Students CRUD (backend + tests) | G1, G2 |
| 5 | Courses CRUD (backend + tests) | G1, G2 |
| 6 | Lectures CRUD (backend + tests) | G1, G2 |
| 7 | Assignments CRUD (backend + tests) | G1, G2 |
| 8 | Submissions + Grading (backend + tests) | G1, G2 |
| 9 | File upload/download (backend + tests) | G1, G2 |
| 10 | Dashboards (backend + tests) | G1, G2 |
| 11 | Frontend: Groups, Teachers, Students pages | G3 |
| 12 | Frontend: Courses list + detail + create | G3 |
| 13 | Frontend: Lectures create/view | G3 |
| 14 | Frontend: Assignments create/submit/grade | G3 |
| 15 | Frontend: Dashboards | G3 |
| 16 | E2E tests | G4 |
| 17 | Docker + CI check | G5 |
| 18 | Review + merge | — |

---

## 8. Dependencies

- AuthService (existing) — for authentication, role check, `User.GetUserId()`
- No other external services needed

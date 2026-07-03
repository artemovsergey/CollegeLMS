# Auth Feature Redesign — Design Spec

**Date:** 2026-07-03
**Feature:** AuthService — Authentication & Authorization (UC-7 through UC-11)
**Approach:** Полный vertical slice с нуля, перезапись в master (Approach B)

---

## 1. Требования

Те же 5 User Stories, без изменений:

| UC | Title | API |
|----|-------|-----|
| UC-7 | Пользователь входит в систему | `POST /api/auth/login` |
| UC-8 | Пользователь выходит из системы | Client-side (localStorage) |
| UC-9 | Пользователь получает свой профиль | `GET /api/auth/profile` |
| UC-10 | Администратор управляет пользователями | `GET/POST/PUT/DELETE /api/users` |
| UC-11 | Администратор меняет роль | `PATCH /api/users/{id}/role` |

Roles: `Admin`, `Teacher`, `Student`, `Dispatcher`

---

## 2. Backend (Phase 1)

### Entity: User
```
User : Entity
  Id (Guid)             — ValueGeneratedNever
  Email (string)        — HasMaxLength(256), unique index
  PasswordHash (string) — HasMaxLength(500)
  FullName (string)     — HasMaxLength(200)
  Role (UserRole)       — HasConversion<string>, HasMaxLength(50)
  IsActive (bool)       — default true
  CreatedAt (DateTime)  — from Entity base
  UpdatedAt (DateTime)  — from Entity base
```

### Enum: UserRole
`Admin`, `Teacher`, `Student`, `Dispatcher`

### EF Configuration
- `ToTable("users")`
- Snake_case via `EFCore.NamingConventions`
- `HasData()` for seed (admin with BCrypt-hashed password)
- `HasIndex` with custom names
- `HasMaxLength` on all string props
- `HasConversion<string>` + `HasMaxLength(50)` on Role enum

### DbConstraints.cs
- CHECK constraint: `role IN ('Admin','Teacher','Student','Dispatcher')`

### Services

#### ITokenService → JwtTokenService
- `GenerateAccessToken(User)` → JWT with NameIdentifier, Role, Jti claims
- HS256, 24h expiry
- Reads key from `config["Jwt:Key"]`

#### IAuthService → AuthService
- `LoginAsync(LoginRequest, ct)` → `Result<LoginResponse>`
- `GetProfileAsync(Guid userId, ct)` → `Result<UserResponse>`

#### IUserService → UserService
- `GetAllAsync(ct)` → `Result<List<UserResponse>>`
- `GetByIdAsync(Guid id, ct)` → `Result<UserResponse>`
- `CreateAsync(CreateUserRequest, ct)` → `Result<UserResponse>`
- `UpdateAsync(Guid id, UpdateUserRequest, ct)` → `Result<UserResponse>`
- `DeleteAsync(Guid id, ct)` → `Result` (soft delete: IsActive = false)
- `ChangeRoleAsync(Guid id, ChangeRoleRequest, ct)` → `Result<UserResponse>`

### Controllers

#### AuthController (`/api/auth`)
| Method | Endpoint | Auth | Swagger Summary |
|--------|----------|------|-----------------|
| POST | /api/auth/login | AllowAnonymous | Вход в систему |
| GET | /api/auth/profile | Authorize | Получить профиль текущего пользователя |

#### UserController (`/api/users`)
| Method | Endpoint | Auth | Roles |
|--------|----------|------|-------|
| GET | /api/users | Authorize | any |
| GET | /api/users/{id} | Authorize | any |
| POST | /api/users | Authorize | Admin |
| PUT | /api/users/{id} | Authorize | Admin |
| DELETE | /api/users/{id} | Authorize | Admin |
| PATCH | /api/users/{id}/role | Authorize | Admin |

### DTOs
- `LoginRequest { Email, Password }`
- `LoginResponse { Token, User: UserResponse }`
- `UserResponse { Id, Email, FullName, Role, IsActive }`
- `CreateUserRequest { Email, Password, FullName, Role }`
- `UpdateUserRequest { Email, FullName, Role }`
- `ChangeRoleRequest { Role }`

### Mapper: UserMapper
- `ToDto(this User)` → `UserResponse`
- `ToEntity(this CreateUserRequest)` → `User` (with BCrypt hashing)

### Validators (FluentValidation, Russian messages)
- `LoginRequestValidator`: Email NotEmpty, Password NotEmpty
- `CreateUserRequestValidator`: Email NotEmpty+Email, Password NotEmpty+Length(6,100), FullName NotEmpty+MaxLength(200), Role NotEmpty
- `UpdateUserRequestValidator`: Email NotEmpty+Email, FullName NotEmpty+MaxLength(200), Role NotEmpty
- `ChangeRoleRequestValidator`: Role NotEmpty

### Swagger
- XML comments on all controllers with `<summary>`, `<remarks>`, `<param>`
- `[SwaggerOperation]`, `[SwaggerResponse]`, `[ProducesResponseType]`
- Examples: `LoginResponseExample`, `UserResponseExample`, `ErrorResponseExample`

---

## 3. Tests (Phase 2) — TDD

### Unit Tests (xUnit + Moq + Bogus)

#### AuthServiceTests
- `Login_ValidCredentials_ReturnsTokenAndUser`
- `Login_InvalidEmail_Returns401`
- `Login_WrongPassword_Returns401`
- `Login_DeactivatedUser_Returns403`
- `GetProfile_ExistingUser_ReturnsUser`
- `GetProfile_NonExistingUser_Returns404`

#### UserServiceTests
- `GetAll_ReturnsAllUsers`
- `GetById_ExistingUser_ReturnsUser`
- `GetById_NonExistingUser_Returns404`
- `Create_UniqueEmail_ReturnsCreatedUser`
- `Create_DuplicateEmail_Returns409`
- `Update_ExistingUser_ReturnsUpdated`
- `Update_DuplicateEmail_Returns409`
- `Delete_ExistingUser_Deactivates`
- `Delete_NonExistingUser_Returns404`
- `ChangeRole_ExistingUser_ChangesRole`

### Integration Tests (WebApplicationFactory)

#### AuthControllerTests
- `POST_Login_Valid_Returns200`
- `POST_Login_Invalid_Returns401`
- `GET_Profile_Authorized_Returns200`
- `GET_Profile_Unauthorized_Returns401`

#### UserControllerTests
- `GET_Users_Authorized_Returns200`
- `GET_Users_Unauthorized_Returns401`
- `POST_Users_Admin_Returns200`
- `POST_Users_NonAdmin_Returns403`
- Full CRUD flow for Admin role

### Fixtures: UserFixture (Bogus)

---

## 4. Frontend (Phase 3) — shadcn/ui Design System

### Components used
From `components/ui/`: `Button`, `Input`, `Card`, `Table`, `Badge`, `Dialog`, `Select`, `Label`, `DropdownMenu`, `sonner`

### Pages

#### `/login`
- `Card` centered on screen
- `Email` + `Password` Inputs with Labels
- Submit Button with loading state
- Error toast via `sonner` or inline error message
- `loading.tsx` — spinner
- `error.tsx` — error boundary with retry

#### `/` (Users Page)
- Header with system name, user email, role Badge, logout Button
- Users Table with columns: Email, FullName, Role, Status, Actions (admin)
- Role column: Badge for display, DropdownMenu for admin to change role
- Actions: Edit (Dialog) + Deactivate (with confirm)
- Create user Dialog with form fields
- Loading spinner, empty state, error state
- Redirect to `/login` if not authenticated

### Auth Infrastructure
- `AuthProvider` wrapping `layout.tsx`
- `useAuth()` hook: `{ user, token, login, logout, isLoading, isAdmin }`
- Axios instance (`lib/api.ts`) with interceptors

### States per page
- Loading: spinner
- Empty: "Нет пользователей"
- Error: toast + inline error message
- Edge cases: expired token → auto-logout, network error → retry

---

## 5. E2E (Phase 4) — Playwright

- Login flow: valid → redirect to `/`, invalid → error message
- Users list: table renders with data
- Create user (admin): dialog → fill → submit → table updates
- Change role: dropdown → select → table updates
- Deactivate user: button → confirm → user becomes inactive
- Logout: button → redirect to `/login`
- Unauthorized access: visit `/` without token → redirect to `/login`

---

## 6. Docs (Phase 5)

- ER diagram: `docs/diagrams/er/user.puml`
- Class diagram: `docs/diagrams/class/user-service.puml`
- Sequence diagrams: `docs/diagrams/sequence/login.puml`, `get-users.puml`
- Security threat model: `docs/diagrams/threat-model/auth-threat-model.md`

---

## 7. DevOps (Phase 6)

- `docker compose build` — verify all services start
- CI/CD check — `.github/workflows` updated

---

## 8. Review & Merge (Phase 7)

- `dotnet build` (G1)
- `dotnet test` (G2)
- `npm run dev` (G3)
- `npx playwright test` (G4)
- `docker compose build` (G5)
- Verification before completion
- Commit to master

---

## 9. Process Gates

| Gate | Check | Phase |
|------|-------|-------|
| G1 | `dotnet build` | 1 |
| G2 | `dotnet test` | 2 |
| G3 | `npm run dev` | 3 |
| G4 | `npx playwright test` | 4 |
| G5 | `docker compose build` | 6 |

# Design: Feedback Tests + WordPress Import Completion

## UC-5: Feedback — Tests

### Unit tests (FeedbackServiceTests)

Following the pattern of `NewsServiceTests`:

| Test | Assertion |
|------|-----------|
| `CreateAsync_ReturnsOk_WithValidRequest` | Feedback saved, `Result.Ok` returned |
| `CreateAsync_Returns429_WhenDuplicateWithin5Minutes` | Same email within 5 min → 429 |
| `CreateAsync_ReturnsOk_SameEmailAfter5Minutes` | Same email after 5 min → success |

### Integration tests (FeedbackControllerTests)

Following the pattern of `NewsControllerTests` (BaseIntegrationTest + WebApplicationFactory + InMemory):

| Test | Assertion |
|------|-----------|
| `Create_Returns200_WithValidRequest` | POST /api/feedback → 200 OK |
| `Create_Returns400_WhenEmailInvalid` | Bad email → 400 from FluentValidation |
| `Create_Returns429_WhenTooFrequent` | Rapid same-email requests → 429 |

No changes to FeedbackService or FeedbackController — only tests.

---

## UC-6: WordPress Import — Completion

### 1. Background import with progress polling

**Current state:** `POST /api/import/wordpress` runs synchronously — client waits until import finishes. No progress feedback.

**New design:**

**POST /api/import/wordpress** (JSON file import) — returns immediately with `{ importId }`, runs import in background (`Task.Run`).

**POST /api/import/wordpress/rest** (live WordPress REST API) — same pattern, fetches from `https://stvcc.ru/wp-json/wp/v2/` instead of reading a JSON file.

**GET /api/import/wordpress/status/{importId}** — returns current progress.

### ImportProgress DTO

```csharp
public class ImportProgressDto
{
    public string ImportId { get; set; }
    public string Status { get; set; } // "running" | "completed" | "failed"
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; set; } = [];
    public ImportResult? Result { get; set; } // set on "completed"
}
```

### Storage

`ConcurrentDictionary<string, ImportProgress>` inside `WordPressImportService` registered as Singleton. Each import gets a unique GUID key. Cleanup: remove entries older than 30 minutes on each new import.

### 2. WordPressImportService refactoring

Extract shared logic to reduce duplication:

- `ImportCategoriesAsync(List<JsonElement> categories, ...)` — processes categories from JSON array
- `ImportPostsAsync(List<JsonElement> posts, ...)` — processes posts, calls shared import
- `ImportFromJsonAsync` — reads file → parses JSON → calls shared methods
- `ImportFromRestApiAsync` — fetches `GET /wp-json/wp/v2/categories` and `GET /wp-json/wp/v2/posts?per_page=100` → calls same shared methods

Both methods run inside `Task.Run` and update the progress dictionary.

### 3. Frontend — Admin Import page

**Page:** `frontend/app/admin/import/page.tsx`

Features:
- Two buttons: "Импорт из JSON" и "Импорт с WordPress (live)"
- After clicking: polling GET `/api/import/wordpress/status/{importId}` every 2 seconds
- Progress bar (HTML `<progress>` or shadcn-style div)
- Counters: "Обработано: {processed} из {total}", "Ошибок: {errors}"
- On completion: show result table (categoriesCreated, postsImported, postsSkipped, errors list)
- Loading spinner during polling
- Error state if polling fails

### 4. Admin navigation

Add to `navItems` in `admin/layout.tsx`:
```typescript
{ href: "/admin/import", label: "Импорт", roles: ["Admin"] }
```

### 5. Files to create/modify

| File | Action | Description |
|------|--------|-------------|
| `CollegeLMS.Tests/Unit/Services/FeedbackServiceTests.cs` | Create | 3 unit tests |
| `CollegeLMS.Tests/Integration/Controllers/FeedbackControllerTests.cs` | Create | 3 integration tests |
| `CollegeLMS.API/Dtos/ImportProgressDto.cs` | Create | DTO for progress |
| `CollegeLMS.API/Services/WordPressImportService.cs` | Modify | Add REST API import, background execution, progress tracking |
| `CollegeLMS.API/Interfaces/IWordPressImportService.cs` | Modify | Add new methods + ImportProgress record |
| `CollegeLMS.API/Controllers/ImportController.cs` | Modify | Add POST /wordpress/rest, GET /wordpress/status/{id} |
| `CollegeLMS.API/SwaggerExamples/ImportResponseExample.cs` | Create | Swagger example for ImportResult |
| `frontend/types/index.ts` | Modify | Add ImportResult, ImportProgressDto types |
| `frontend/app/admin/import/page.tsx` | Create | Admin import page |
| `frontend/app/admin/layout.tsx` | Modify | Add nav link |

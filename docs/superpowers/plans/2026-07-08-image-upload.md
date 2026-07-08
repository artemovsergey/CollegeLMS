# Image Upload & Poster Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add image upload with automatic poster generation for news articles.

**Architecture:** Separate `POST /api/upload` endpoint processes image (resize + center-crop to 1200×600, JPEG q85), saves to shared Docker volume, returns URL. Frontend auto-uploads on file select, stores returned URL as `imageUrl`. Nginx serves uploads as static files.

**Tech Stack:** SixLabors.ImageSharp, Docker named volume, nginx static serving

## Global Constraints
- Upload max 10 MB, MIME types: image/jpeg, image/png
- Poster output: JPEG quality 85, max width 1200px, center-crop height 600px
- No upscaling (if source < 1200×600, pad with white)
- All new endpoints require `[Authorize(Roles = "Admin")]`
- Error messages in Russian
- Follow existing code patterns (primary constructors, `Result<T>`, Swagger annotations)

---

### Task 1: Docker Compose — add uploads_data volume

**Files:**
- Modify: `docker-compose.yml`

**Interfaces:**
- Consumes: nothing
- Produces: named volume `uploads_data` available to `api` and `nginx` services

- [ ] **Step 1: Add volume to docker-compose.yml**

Add `uploads_data:` to the `volumes:` block and mount it in both `api` and `nginx` services:

```yaml
volumes:
  postgres_data:
  redis_data:
  nuget_packages:
  uploads_data:
```

In `api` service:
```yaml
  api:
    volumes:
      - ./import:/import
      - uploads_data:/app/uploads
```

In `nginx` service, add a `volumes:` block:
```yaml
  nginx:
    volumes:
      - uploads_data:/var/www/uploads
```

- [ ] **Step 2: Verify no syntax errors**

Run: `docker compose config`
Expected: outputs merged compose config without errors

- [ ] **Step 3: Commit**

```bash
git add docker-compose.yml
git commit -m "infra: add uploads_data volume for image uploads"
```

### Task 2: Nginx — add /uploads/ location

**Files:**
- Modify: `nginx/nginx.conf`

**Interfaces:**
- Produces: serves `/uploads/` from `/var/www/uploads/`, cache headers

- [ ] **Step 1: Add location /uploads/ to nginx.conf**

Add before the final closing `}` of the `server` block:

```nginx
        location /uploads/ {
            alias /var/www/uploads/;
            expires 30d;
            add_header Cache-Control "public, immutable";
        }
```

Verify the full file looks correct:
```nginx
events {
    worker_connections 1024;
}

http {
    upstream api {
        server api:8080;
    }

    upstream frontend {
        server frontend:3000;
    }

    server {
        listen 80;
        server_name _;
        client_max_body_size 100M;

        location /api/ {
            proxy_pass http://api;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        location /swagger/ {
            proxy_pass http://api/swagger/;
            proxy_set_header Host $host;
        }

        location /uploads/ {
            alias /var/www/uploads/;
            expires 30d;
            add_header Cache-Control "public, immutable";
        }

        location / {
            proxy_pass http://frontend;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add nginx/nginx.conf
git commit -m "infra: add /uploads/ location to nginx"
```

### Task 3: Backend — add ImageSharp + UploadController

**Files:**
- Modify: `CollegeLMS.API/CollegeLMS.csproj`
- Create: `CollegeLMS.API/Controllers/UploadController.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Produces: `POST /api/upload` returns `{ "url": "/uploads/news/{guid}.jpg" }`

- [ ] **Step 1: Add SixLabors.ImageSharp to csproj**

Add to the <!-- Logging --> section or create a new <!-- Image Processing --> section:

```xml
  <!-- Image Processing -->
  <ItemGroup>
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.7" />
  </ItemGroup>
```

- [ ] **Step 2: Create UploadController**

```csharp
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/upload")]
[Produces("application/json")]
public class UploadController : ControllerBase
{
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private const int TargetWidth = 1200;
    private const int TargetHeight = 600;
    private const int JpegQuality = 85;

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Загрузить изображение для новости (только Admin)")]
    [SwaggerResponse(200, "Изображение загружено", typeof(Result))]
    [SwaggerResponse(400, "Некорректный файл")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<Result>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(Result.Fail("Файл не выбран", 400));

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return BadRequest(Result.Fail("Разрешены только JPEG и PNG", 400));

        var fileId = Guid.NewGuid();
        var uploadsDir = Path.Combine("uploads", "news");
        Directory.CreateDirectory(uploadsDir);

        var outputPath = Path.Combine(uploadsDir, $"{fileId}.jpg");

        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream, ct);

        // Downscale width to 1200px max (preserve aspect ratio, don't upscale)
        if (image.Width > TargetWidth)
        {
            var ratio = (double)TargetWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(TargetWidth, newHeight));
        }

        // Center-crop height to 600px
        if (image.Height > TargetHeight)
        {
            var cropY = (image.Height - TargetHeight) / 2;
            image.Mutate(x => x.Crop(new Rectangle(0, cropY, image.Width, TargetHeight)));
        }
        else if (image.Height < TargetHeight)
        {
            // Pad with white if shorter than target
            var padding = (TargetHeight - image.Height) / 2;
            var paddedWidth = image.Width;
            image.Mutate(x =>
                x.Resize(new ResizeOptions
                {
                    Size = new Size(paddedWidth, TargetHeight),
                    Mode = ResizeMode.BoxPad,
                    PadColor = Color.White,
                })
            );
        }

        var encoder = new JpegEncoder { Quality = JpegQuality };
        await image.SaveAsync(outputPath, encoder, ct);

        var url = $"/uploads/news/{fileId}.jpg";

        return Ok(Result<object>.Ok(new { url }));
    }
}
```

Wait — I need to check if `Result<object>` is the right pattern. Let me check the existing response types.

Looking at existing controllers, they return `Result<T>` directly. For this endpoint that doesn't have a DTO, I should create a simple response DTO. Let me use a record:

Actually, I'll just return an anonymous object wrapped in Result. Let me check the existing pattern...

Looking at the AuthController or similar, the pattern is `Result<LoginResponse>`. For the upload, I'll create a simple `UploadResponse` record.

Let me fix the controller:

```csharp
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/upload")]
[Produces("application/json")]
public class UploadController : ControllerBase
{
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png"];
    private const long MaxFileSize = 10 * 1024 * 1024;

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Загрузить изображение для новости (только Admin)")]
    [SwaggerResponse(200, "Изображение загружено", typeof(Result<UploadResponse>))]
    [SwaggerResponse(400, "Некорректный файл", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<UploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<Result<UploadResponse>>> Upload(
        IFormFile file,
        CancellationToken ct
    )
    {
        if (file is null || file.Length == 0)
            return BadRequest(Result<UploadResponse>.Fail("Файл не выбран", 400));

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return BadRequest(Result<UploadResponse>.Fail("Разрешены только JPEG и PNG", 400));

        var fileId = Guid.NewGuid();
        var uploadsDir = Path.Combine("uploads", "news");
        Directory.CreateDirectory(uploadsDir);

        var outputPath = Path.Combine(uploadsDir, $"{fileId}.jpg");

        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream, ct);

        // Downscale width to 1200px max (preserve aspect ratio, don't upscale)
        if (image.Width > 1200)
        {
            var ratio = (double)1200 / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(1200, newHeight));
        }

        // Center-crop height to 600px (pad with white if too short)
        if (image.Height > 600)
        {
            var cropY = (image.Height - 600) / 2;
            image.Mutate(x => x.Crop(new Rectangle(0, cropY, image.Width, 600)));
        }
        else if (image.Height < 600)
        {
            image.Mutate(x =>
                x.Resize(new ResizeOptions
                {
                    Size = new Size(image.Width, 600),
                    Mode = ResizeMode.BoxPad,
                    PadColor = Color.White,
                })
            );
        }

        var encoder = new JpegEncoder { Quality = 85 };
        await image.SaveAsync(outputPath, encoder, ct);

        var url = $"/uploads/news/{fileId}.jpg";

        return Ok(Result<UploadResponse>.Ok(new UploadResponse { Url = url }));
    }
}

public class UploadResponse
{
    public string Url { get; set; } = string.Empty;
}
```

Hmm, but `UploadResponse` should go in a Dtos file. Let me put it in a separate Dtos file.

Actually, looking at the codebase structure, DTOs are in `CollegeLMS.API/Dtos/`. Let me create a small `UploadResponse.cs` there.

Wait, or I can just do what other controllers do — put it inline in the same namespace in the controller file. But the convention is to have DTOs in the Dtos folder. Let me create a small file.

Let me refactor:
- Create: `CollegeLMS.API/Dtos/UploadResponse.cs`
- Create: `CollegeLMS.API/Controllers/UploadController.cs`

- [ ] **Step 3: Create UploadResponse DTO**

```csharp
namespace CollegeLMS.API.Dtos;

public class UploadResponse
{
    public string Url { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Create UploadController**

As shown above.

- [ ] **Step 5: Register in DI (nothing needs registration — controller is auto-discovered)**

Controllers are auto-discovered by `AddControllers()`. No additional DI registration needed.

- [ ] **Step 6: Build and verify**

Run: `docker compose exec api dotnet build --no-restore`
Expected: Build succeeded, 0 warnings, 0 errors

- [ ] **Step 7: Commit**

```bash
git add CollegeLMS.API/
git commit -m "feat: add image upload endpoint with ImageSharp processing"
```

### Task 4: Frontend — replace URL input with file upload

**Files:**
- Modify: `frontend/app/admin/news/page.tsx`
- Modify: `frontend/types/index.ts`

**Interfaces:**
- Consumes: `POST /api/upload` (returns `{ url }`)
- Consumes: `POST /api/news` (existing)

- [ ] **Step 1: Add UploadResponse type**

Add to `frontend/types/index.ts`:

```typescript
export interface UploadResponse {
  url: string
}
```

- [ ] **Step 2: Modify admin news form**

Replace the URL input section and add upload logic:

1. Add `uploading` state variable: `const [uploading, setUploading] = useState(false)`
2. Add upload handler function
3. Replace the URL `<Input>` with file `<Input>` + preview

The existing URL input:
```tsx
      <div className="flex flex-col gap-2">
        <Label htmlFor="news-image">URL изображения</Label>
        <Input id="news-image" value={formImageUrl} onChange={e => setFormImageUrl(e.target.value)} placeholder="https://..." />
      </div>
```

Replace with:
```tsx
      <div className="flex flex-col gap-2">
        <Label htmlFor="news-image">Изображение (постер)</Label>
        <div className="flex items-center gap-2">
          <Input
            id="news-image"
            type="file"
            accept="image/jpeg,image/png"
            disabled={uploading}
            onChange={async e => {
              const file = e.target.files?.[0]
              if (!file) return
              setUploading(true)
              setFormError(null)
              try {
                const formData = new FormData()
                formData.append("file", file)
                const res = await api.post<Result<UploadResponse>>("/api/upload", formData)
                if (res.data.isSuccess && res.data.data) {
                  setFormImageUrl(res.data.data.url)
                } else {
                  setFormError(res.data.errorMessage ?? "Ошибка загрузки")
                }
              } catch {
                setFormError("Ошибка загрузки файла")
              } finally {
                setUploading(false)
              }
            }}
          />
          {uploading && (
            <div className="h-5 w-5 animate-spin rounded-full border-2 border-muted border-t-primary shrink-0" />
          )}
        </div>
        {formImageUrl && (
          <div className="relative mt-1 overflow-hidden rounded-md">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={formImageUrl}
              alt="Превью"
              className="h-32 w-full object-cover"
            />
          </div>
        )}
      </div>
```

The import for `UploadResponse` is needed — add it to the import line:
```tsx
import type {
  Result,
  NewsResponse,
  NewsCategoryResponse,
  CreateNewsRequest,
  UpdateNewsRequest,
  PagedResponse,
  UploadResponse,
} from "@/types"
```

Axios handles FormData automatically — no need to manually set Content-Type.

- [ ] **Step 4: Verify build**

Run: `npm run build` in `frontend/`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add frontend/
git commit -m "feat: replace URL input with file upload in admin news form"
```

### Task 5: Build and verify end-to-end

- [ ] **Step 1: Rebuild all containers**

```bash
docker compose up -d --build
```
Expected: all 5 containers start healthy

- [ ] **Step 2: Verify upload endpoint**

```bash
# Create a test image
docker compose exec api bash -c "apt-get update && apt-get install -y imagemagick || true"
# or simpler: create a small test image with curl
# Upload test
curl -X POST http://localhost/api/upload \
  -H "Authorization: Bearer <token>" \
  -F "file=@test.jpg"
```
Expected: returns `{ "url": "/uploads/news/{guid}.jpg" }`

- [ ] **Step 3: Verify nginx serves the file**

```bash
curl -I http://localhost/uploads/news/{guid}.jpg
```
Expected: 200 OK, content-type image/jpeg

- [ ] **Step 4: Verify frontend flow**

Login as admin → create news → select file → verify upload completes → submit → news appears with poster

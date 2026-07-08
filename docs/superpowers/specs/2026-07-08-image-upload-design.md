# Image Upload & Poster — Design Spec

## Purpose
Replace textual `ImageUrl` input in news creation with file upload; automatically generate an optimized poster image from the uploaded file.

## Scope
- Backend: upload endpoint, image processing (ImageSharp), file storage
- Frontend: file picker + preview in admin news form
- Infrastructure: Docker named volume, nginx static serving

## Non-goals
- Bake text overlays into the image (stays in CSS)
- Multiple image uploads
- Image deletion/cleanup on news delete

## Architecture

```
Frontend                         Backend                        Nginx
   │                               │                              │
   │ POST /api/upload (file)      │                              │
   │─────────────────────────────>│                              │
   │                               │ resize + crop               │
   │                               │ save to /app/uploads/       │
   │< { url: "/uploads/news/..." } │                              │
   │                               │                              │
   │ POST /api/news (JSON + url)  │                              │
   │─────────────────────────────>│                              │
   │                               │ store ImageUrl in DB        │
   │< { news }                     │                              │
   │                               │                              │
   │ GET /uploads/news/{file}     │                              │
   │────────────────────────────────────────────────────────────>│
   │< file                                                       │
```

## Backend

### Upload endpoint
- `POST /api/upload`
- Accepts `multipart/form-data` with field `file`
- Validates: file size ≤ 10 MB, MIME type `image/jpeg` or `image/png`
- Processes image:
  - Downscale width to 1200px max (maintain aspect ratio, don't upscale)
  - Center-crop height to 600px (if height < 600, pad with white)
  - Convert to JPEG quality 85
- Saves to `uploads/news/{guid}.jpg`
- Returns `{ "url": "/uploads/news/{guid}.jpg" }`

### Image library
- Add `SixLabors.ImageSharp` NuGet package
- Processing: `Load → Resize(1200, height*1200/width) → Crop(0, (h-600)/2, 1200, 600) → SaveAsJpeg(quality=85)`

### News entity
- No schema change — `ImageUrl` continues as `string?`
- Upload flow stores local path instead of external URL

### DI registration
- Register `ImageService` (or inline processing) in `ServiceCollectionExtensions`

## Frontend

### Admin news form (admin/news/page.tsx)
- Replace `<Input id="news-image">` with:
  - `<Input type="file" accept="image/*">` 
  - Image preview when file selected or existing `imageUrl` present
  - Hidden input for external URL fallback (optional)
- Upload flow:
  1. User selects file → auto-upload to `/api/upload`
  2. While uploading: show spinner on preview
  3. On success: set `formImageUrl` = returned URL, show preview
  4. On error: show error toast
  5. User submits form → JSON with `imageUrl` as before

### Types
- Add `UploadResponse { url: string }` to `frontend/types/index.ts`

## Infrastructure

### Docker Compose
- Add named volume `uploads_data`
- API service: mount `uploads_data:/app/uploads`
- Nginx service: mount `uploads_data:/var/www/uploads`

### Nginx
- Add location block:
```nginx
location /uploads/ {
    alias /var/www/uploads/;
    expires 30d;
    add_header Cache-Control "public, immutable";
}
```
- Keep `client_max_body_size 10M;` (100M already set)

### Ordering
1. docker-compose.yml — add volume + mounts
2. nginx/nginx.conf — add /uploads/ location
3. CollegeLMS.API — add ImageSharp + upload endpoint
4. frontend — replace input + upload flow
5. docker compose up -d --build

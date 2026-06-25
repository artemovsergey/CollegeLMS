---
name: docker-compose-dev
description: Set up local development environment with PostgreSQL 16 and Redis via Docker Compose
---

# docker-compose-dev

Configure and manage local development infrastructure (PostgreSQL + Redis) using Docker Compose with named volumes, health checks, and consistent naming.

## Workflow

### 1. Create docker-compose.yml

Path: `docker-compose.yml` (project root)

```yaml
networks:
  collegelms-net:
    driver: bridge

volumes:
  postgres_data:
  redis_data:

services:
  db:
    image: postgres:16-alpine
    container_name: collegelms-db
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: collegelms
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5
    networks:
      - collegelms-net

  redis:
    image: redis:7-alpine
    container_name: collegelms-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    command: redis-server --appendonly yes
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5
    networks:
      - collegelms-net
```

### 2. Configure appsettings

Path: `CollegeLMS.API/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=collegelms;Username=postgres;Password=postgres"
  }
}
```

### 3. Create .dockerignore

Path: `.dockerignore`

```
**/.env
**/.git
**/node_modules
**/bin
**/obj
**/.vs
**/.vscode
```

### 4. First time setup

```powershell
# Start containers
docker compose up -d

# Verify health
docker compose ps

# Apply EF migrations
dotnet ef database update
```

### 5. Daily commands

```powershell
# Start services
docker compose up -d

# Stop (preserves volumes)
docker compose down

# Stop + delete volumes (WARNING: destroys data)
docker compose down -v

# View logs
docker compose logs -f db

# Rebuild after config change
docker compose up -d --force-recreate

# Cleanup (remove unused images, containers, volumes)
docker system prune -a --volumes
```

### 6. Connection string patterns

| Environment | Host | Port | Notes |
|-------------|------|------|-------|
| Local dev | `localhost` | 5432 | API runs on host |
| Docker compose | `db` | 5432 | Service name, not localhost |
| Production | VPS IP | 5432 | From env variable via compose |

### 7. Reset database

```powershell
docker compose down -v
docker compose up -d
dotnet ef database update
```

## Convention rules

- PostgreSQL 16-alpine (matches production target)
- Redis 7-alpine (sessions only per AGENTS.md)
- Named volumes with `_data` suffix
- Health checks on all stateful services
- Container names prefixed `collegelms-`
- Network named `{project}-net`
- Default credentials: `postgres` / `postgres`
- `.dockerignore` excludes build artifacts, secrets, IDE configs

## Verification

- `docker compose ps` — both services healthy
- `dotnet ef database update` — succeeds
- API starts and connects to database
- `redis-cli ping` — returns `PONG`

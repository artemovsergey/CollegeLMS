---
name: docker-compose-dev
description: Set up local development environment with PostgreSQL 16 and Redis via Docker Compose
---

# docker-compose-dev

Create or update `docker-compose.yml` for local development.

## Services

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| `db` | postgres:16-alpine | 5432 | Main database |
| `redis` | redis:7-alpine | 6379 | Session cache |
| `api` | build from CollegeLMS.API/Dockerfile | 5026 | ASP.NET API |
| `collegelms-next` | build from CollegeLMS.Next/Dockerfile | 3000 | Next.js UI |
| `loadbalancer` | custom ./loadbalancer | 80/443 | Reverse proxy |

## docker-compose.yml

```yaml
services:
  db:
    image: postgres:16-alpine
    container_name: collegelms-db
    environment:
      POSTGRES_USER: ${DB_USER:-postgres}
      POSTGRES_PASSWORD: ${DB_PASSWORD:-postgres}
      POSTGRES_DB: ${DB_NAME:-collegelms}
    ports: ["${DB_PORT:-5432}:5432"]
    volumes: [postgres_data:/var/lib/postgresql/data]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: collegelms-redis
    ports: ["${REDIS_PORT:-6379}:6379"]
    volumes: [redis_data:/data]
    command: redis-server --appendonly yes

volumes:
  postgres_data:
  redis_data:
```

## Usage

```bash
# Start all services (always rebuild)
docker compose up --build -d

# Start only DB + Redis (for local dev)
docker compose up -d db redis

# Stop all
docker compose down

# Clean volumes (destroy data)
docker compose down -v
```

## Verification

- `docker compose up -d db` starts Postgres
- `pg_isready -U postgres` returns success
- `redis-cli ping` returns PONG
- API connects to `Host=db;Port=5432` in Docker mode

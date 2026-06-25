---
name: vps-deploy
description: Configure production deployment on VPS with Nginx reverse proxy, Docker Compose, and GitHub Actions CI/CD
---

# vps-deploy

Set up full deployment pipeline: Nginx reverse proxy, Docker Compose production stack, GitHub Actions CI/CD workflows (test + deploy), and deployment scripts.

## Workflow

### 1. Nginx configuration

Path: `nginx/nginx.conf`

```nginx
events {
    worker_connections 1024;
}

http {
    upstream api {
        server api:8080;
    }

    upstream frontend {
        server nextjs:3000;
    }

    server {
        listen 80;
        server_name _;
        client_max_body_size 100M;

        location /api/ {
            proxy_pass http://api/;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        location /swagger/ {
            proxy_pass http://api/swagger/;
            proxy_set_header Host $host;
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

Path: `nginx/Dockerfile`

```dockerfile
FROM nginx:alpine
RUN rm /etc/nginx/conf.d/default.conf
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80 443
```

### 2. Dockerfile for API

Path: `CollegeLMS.API/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CollegeLMS.API/CollegeLMS.API.csproj", "CollegeLMS.API/"]
RUN dotnet restore "CollegeLMS.API/CollegeLMS.API.csproj"
COPY . .
WORKDIR "/src/CollegeLMS.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY appsettings.Production.json .
ENTRYPOINT ["dotnet", "CollegeLMS.API.dll"]
```

### 3. Dockerfile for Next.js

Path: `Dockerfile` (Next.js project root)

```dockerfile
FROM node:22-alpine AS base

FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs
COPY --from=builder /app/public ./public
RUN mkdir .next && chown nextjs:nodejs .next
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
USER nextjs
EXPOSE 3000
ENV PORT=3000
CMD ["node", "server.js"]
```

### 4. Docker Compose production

Path: `docker-compose.prod.yml`

```yaml
networks:
  collegelms-net:
    driver: bridge

volumes:
  postgres_data:

services:
  db:
    image: postgres:16-alpine
    container_name: collegelms-db
    environment:
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
      POSTGRES_DB: ${DB_NAME}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    restart: always
    networks:
      - collegelms-net

  api:
    build:
      context: .
      dockerfile: CollegeLMS.API/Dockerfile
    container_name: collegelms-api
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
    depends_on:
      db:
        condition: service_healthy
    restart: always
    networks:
      - collegelms-net

  nginx:
    build: ./nginx
    container_name: collegelms-nginx
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - api
    restart: always
    networks:
      - collegelms-net
```

### 5. Environment file

Path: `.env.production` (DO NOT commit — add to `.gitignore`)

```
DB_USER=postgres
DB_PASSWORD=<strong-password>
DB_NAME=collegelms
```

### 6. Deploy script

Path: `scripts/deploy.sh`

```bash
#!/bin/bash
set -e

echo "Pulling latest code..."
cd /home/deploy/collegelms
git pull origin master

echo "Building and starting containers..."
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml up -d --build

echo "Running migrations..."
docker compose -f docker-compose.prod.yml exec -T api dotnet ef database update

echo "Deployment complete"
docker compose -f docker-compose.prod.yml ps
```

### 7. GitHub Actions — Test workflow

Path: `.github/workflows/test.yml`

```yaml
name: Test

on:
  push:
    branches: [master, develop]
  pull_request:
    branches: [master]

jobs:
  test:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: collegelms_test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 5s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0"

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build -c Release --collect:"XPlat Code Coverage"
        env:
          ConnectionStrings__DefaultConnection: Host=localhost;Port=5432;Database=collegelms_test;Username=postgres;Password=postgres

      - name: Generate coverage report
        uses: danielpalme/ReportGenerator-GitHub-Action@5
        with:
          reports: "**/coverage.cobertura.xml"
          targetdir: "coverage"

      - name: Deploy coverage to GitHub Pages
        uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./coverage
```

### 8. GitHub Actions — Deploy workflow

Path: `.github/workflows/deploy.yml`

```yaml
name: Deploy

on:
  push:
    branches: [master]

jobs:
  deploy:
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/master'

    steps:
      - uses: actions/checkout@v4

      - name: Deploy to VPS
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USERNAME }}
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          script: |
            cd /home/deploy/collegelms
            git pull origin master
            docker compose -f docker-compose.prod.yml down
            docker compose -f docker-compose.prod.yml up -d --build
            docker compose -f docker-compose.prod.yml exec -T api dotnet ef database update
```

### 9. Required GitHub secrets

| Secret | Description |
|--------|-------------|
| `VPS_HOST` | VPS IP or hostname |
| `VPS_USERNAME` | SSH user (e.g., `deploy`) |
| `SSH_PRIVATE_KEY` | Private SSH key (not password) |
| `GITHUB_TOKEN` | Auto-provided by GitHub |

### 10. Initial VPS setup

```bash
# SSH into VPS
ssh user@ip

# Create deploy user (if not exists)
adduser deploy
usermod -aG docker deploy

# Create project directory
mkdir -p /home/deploy/collegelms

# Generate SSH key pair on local machine
ssh-keygen -t rsa -b 4096 -f ~/.ssh/deploy_key

# Add public key to VPS
ssh-copy-id -i ~/.ssh/deploy_key.pub deploy@ip

# Add private key to GitHub secrets as SSH_PRIVATE_KEY
```

## Convention rules

- API on internal port 8080, Nginx on host 80/443
- SSL termination at Nginx (LetsEncrypt certs mounted externally later)
- Environment variables via `.env.production` or GitHub secrets
- `restart: always` on all production services
- Health checks for DB dependency ordering
- Migrations run as separate step after containers start
- Non-root users in containers (`$APP_UID`, `nextjs`)
- `.dockerignore` to exclude build artifacts
- Container name prefix `collegelms-`

## Useful commands

```bash
# Full cleanup
docker compose -f docker-compose.prod.yml down -v
docker system prune -a --volumes

# Check logs
docker compose -f docker-compose.prod.yml logs -f api

# Exec into container
docker compose -f docker-compose.prod.yml exec api sh
```

## Verification

- `docker compose -f docker-compose.prod.yml config` validates YAML
- `docker compose -f docker-compose.prod.yml build` builds without errors
- Nginx reverse proxy routes `/api/` → API, `/` → Next.js
- GitHub Actions test workflow passes
- GitHub Actions deploy workflow completes without errors

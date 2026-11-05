---
name: cicd-pipeline
description: Set up GitHub Actions CI/CD pipeline — test.yml with .NET build/test, frontend build/lint, CSharpier formatting check
---

# cicd-pipeline

Create or update `.github/workflows/test.yml` for continuous integration.

## Pipeline

```yaml
name: Test

on:
  push:
    branches: [master]
  pull_request:
    branches: [master]

jobs:
  test-api:
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
        run: dotnet restore CollegeLMS.API/CollegeLMS.csproj && dotnet restore CollegeLMS.Tests/CollegeLMS.Tests.csproj

      - name: Restore tools
        run: dotnet tool restore

      - name: Check formatting
        run: dotnet csharpier check .

      - name: Build
        run: dotnet build CollegeLMS.API/CollegeLMS.csproj --no-restore -c Release && dotnet build CollegeLMS.Tests/CollegeLMS.Tests.csproj --no-restore -c Release

      - name: Test
        run: dotnet test CollegeLMS.Tests/CollegeLMS.Tests.csproj --no-build -c Release --collect:"XPlat Code Coverage"
        env:
          ConnectionStrings__DefaultConnection: Host=localhost;Port=5432;Database=collegelms_test;Username=postgres;Password=postgres

  test-frontend:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"

      - name: Install dependencies
        run: npm ci
        working-directory: CollegeLMS.Next

      - name: Lint
        run: npm run lint
        working-directory: CollegeLMS.Next

      - name: Build
        run: npm run build
        working-directory: CollegeLMS.Next
```

## Setup steps

1. Create `.github/workflows/test.yml` with the config above
2. Install CSharpier: `dotnet tool install csharpier` (adds to `.config/dotnet-tools.json`)
3. Create `.csharpier` config file in repo root
4. Verify: `dotnet csharpier . --check`

## Quality gates

| Gate | Command |
|------|---------|
| Formatting | `dotnet csharpier check .` |
| .NET build | `dotnet build` |
| .NET test | `dotnet test` |
| Frontend lint | `npm run lint` |
| Frontend build | `npm run build` |

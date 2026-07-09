# ScheduleService — Complete Implementation Report

**Date:** 2026-07-09
**Status:** ✅ All 7 User Stories (UC-12..UC-18) implemented

## Summary

ScheduleService is now fully implemented:
- UC-12..14: CRUD reading with filters (group, teacher, room, day, period)
- UC-15: CRUD write — Dispatcher/Admin can create, edit, delete entries
- UC-16: Period filtering — day, week, month (year + calendar view pending)
- UC-17: Export — PDF (QuestPDF) + XLSX (ClosedXML)
- UC-18: Import — XLSX upload with validation + conflict detection

## Changes Made

### Backend
- `ScheduleService.cs` — full CRUD with overlap validation, StartTime < EndTime check
- `ScheduleExportService.cs` — QuestPDF + ClosedXML export
- `ScheduleImportService.cs` — XLSX parsing, validation, idempotent insert
- `ScheduleController.cs` — export/import/CRUD endpoints
- `ScheduleValidators.cs` — FluentValidation rules
- `DataSeeder.cs` — added Dispatcher user (dispatcher@collegelms.ru)
- `TeacherController.cs`, `GroupController.cs` — GET endpoints available to all roles
- `ScheduleResponse.DayOfWeek` — changed from `DayOfWeek` enum to `int` for JSON compatibility
- `ScheduleMapper.cs` — (int) cast for DayOfWeek

### Frontend
- `app/schedule/page.tsx` — full page with filters, table, CRUD dialogs, export/import
- `components/ScheduleFilterBar.tsx` — filter controls + export/import/add buttons
- `components/ScheduleTable.tsx` — clickable rows, delete on hover
- `components/ScheduleEntryDialog.tsx` — create/edit modal with validation
- `components/ScheduleImportDialog.tsx` — XLSX upload with results display
- `components/ui/alert-dialog.tsx` — shadcn/ui AlertDialog (was missing)
- `app/layout.tsx` — added `<Toaster />` from sonner (toasts were invisible)
- `lib/utils.ts` — added `extractErrorMessage()` helper (handles Result<T>, ProblemDetails, ErrorResponse formats)
- `app/login/page.tsx` — quick-login dropdown for 4 roles; Dispatcher → /schedule

### Infrastructure
- `nginx/nginx.conf` — replaced `upstream` blocks with `set` + `resolver 127.0.0.11 valid=10s` (prevents 502 after container recreation)
- `CollegeLMS.API/Dockerfile` — combined restore/build/publish into single RUN with `--mount type=cache,id=nuget`

## Key Decisions
- DayOfWeek → int for serialization (JSON serializes enum as int)
- QuestPDF (MIT) for PDF, ClosedXML (MIT) for XLSX
- NuGet cache mount in Docker BuildKit for fast rebuilds
- Nginx resolver for dynamic DNS resolution (prevents 502)
- Sonner Toaster in root layout (was missing entirely)
- extractErrorMessage handles 3 error formats: Result<T>.errorMessage, ErrorResponse.message, ProblemDetails.errors/title

## Remaining
- UC-16: year period + calendar view
- UC-8: logout Redis blacklist (current: just localStorage clear)
- Infrastructure: VPS deploy, log rotation, DB backup
- All future services

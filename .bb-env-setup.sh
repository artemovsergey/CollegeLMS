#!/usr/bin/env bash
# Подготовка нового bb-worktree для CollegeLMS.
# bb запускает скрипт после `git worktree add` и копирования `.worktreeinclude`.
# Шаги best-effort: сбой установки зависимостей не должен удалять worktree.
set -uo pipefail

log() { printf '[bb-env-setup] %s\n' "$*"; }

run() {
  local name="$1"
  shift
  log "→ $name"
  if "$@"; then
    log "✓ $name"
  else
    log "⚠ $name завершился с ошибкой — продолжаю"
  fi
}

if ! command -v dotnet >/dev/null 2>&1; then
  log "⚠ dotnet не найден в PATH"
else
  run "dotnet tool restore" dotnet tool restore
  run "dotnet restore CollegeLMS.slnx" dotnet restore CollegeLMS.slnx
fi

if [ -f CollegeLMS.Next/package.json ]; then
  if command -v npm >/dev/null 2>&1; then
    run "npm ci (CollegeLMS.Next)" npm --prefix CollegeLMS.Next ci
  else
    log "⚠ npm не найден в PATH"
  fi
fi

log "Готово"
exit 0

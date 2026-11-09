<#
.SYNOPSIS
    Устанавливает автоматизацию анализа сессий DeepSeek Harness.

.DESCRIPTION
    Идемпотентно:
      1. копирует анализатор, лаунчер и README в ~/.dsh/session-analytics;
      2. регистрирует задачу планировщика «DSH Session Analytics»
         (при входе и каждые N минут);
      3. выполняет первый прогон.

    Источник — scripts/dsh/session-analytics.mjs. Данные и отчёты остаются
    в ~/.dsh/session-analytics и в git не попадают.

.EXAMPLE
    pwsh -File scripts/dsh/install-session-analytics.ps1
    pwsh -File scripts/dsh/install-session-analytics.ps1 -Full -SkipRun
#>
[CmdletBinding()]
param(
    [switch]$SkipTask,
    [switch]$SkipRun,
    [switch]$Full,
    [int]$IntervalMinutes = 10
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sources = [ordered]@{
    'session-analytics.mjs'          = 'session-analytics.mjs'
    'session-analytics.cmd'          = 'run.cmd'
    'session-analytics-background.ps1' = 'run-background.ps1'
    'session-analytics.md'           = 'README.md'
}

$target = Join-Path $env:USERPROFILE '.dsh\session-analytics'
New-Item -ItemType Directory -Force -Path $target, (Join-Path $target 'data\sessions'), (Join-Path $target 'reports') | Out-Null

foreach ($pair in $sources.GetEnumerator()) {
    $source = Join-Path $scriptDir $pair.Key
    if (-not (Test-Path $source)) { throw "Не найден файл: $source" }
    Copy-Item $source (Join-Path $target $pair.Value) -Force
}
Write-Host "Установлено: $target"

if (-not $SkipTask) {
    $taskName = 'DSH Session Analytics'
    # schtasks работает без повышения прав; Register-ScheduledTask и задача
    # с триггером ONLOGON требуют администратора.
    $taskRun = '"powershell.exe" -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "' +
        (Join-Path $target 'run-background.ps1') + '"'

    try {
        schtasks /Create /TN $taskName /TR $taskRun /SC MINUTE /MO $IntervalMinutes /F | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "schtasks завершился с кодом $LASTEXITCODE" }
        Write-Host "Задача зарегистрирована: «$taskName» (каждые $IntervalMinutes мин)"
    }
    catch {
        Write-Warning "Не удалось зарегистрировать задачу: $($_.Exception.Message)"
        Write-Host 'Отчёты можно обновлять вручную: run.cmd'
    }
}

if (-not $SkipRun) {
    $runCmd = Join-Path $target 'run.cmd'
    if ($Full) {
        & $runCmd --reindex
    }
    else {
        & $runCmd
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Анализатор завершился с кодом $LASTEXITCODE"
    }
}

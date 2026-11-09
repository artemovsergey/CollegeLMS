<#
.SYNOPSIS
    Собирает локальный .env из приватного репозитория secrets.

.DESCRIPTION
    Источники (в порядке возрастания приоритета):
      1. .env              — общие секреты;
      2. collegelms.env    — переменные проекта CollegeLMS.

    Локальные переменные, которых нет в secrets (например, OPENCODE_*),
    сохраняются. Значения никогда не выводятся в консоль — только имена
    ключей и количество изменений.

    Формат: KEY=value без кавычек и пробелов вокруг "=".
    Файл .env остаётся в .gitignore.

.EXAMPLE
    pwsh -File scripts/dsh/sync-secrets.ps1
#>
[CmdletBinding()]
param(
    [string]$Repo = 'artemovsergey/secrets',
    [string]$Target = (Join-Path (Get-Location) '.env')
)

$ErrorActionPreference = 'Stop'

function Read-RemoteEnv([string]$Path) {
    $text = & gh api -H 'Accept: application/vnd.github.raw' "repos/$Repo/contents/$Path" 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $text) {
        throw "Не удалось прочитать $Path из $Repo (проверьте gh auth status)"
    }
    return ($text -join "`n")
}

function Parse-Env([string]$Text) {
    $map = [ordered]@{}
    foreach ($line in ($Text -split "`r?`n")) {
        $trimmed = $line.Trim()
        if ($trimmed -match '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$') {
            $map[$matches[1]] = $matches[2]
        }
    }
    return $map
}

Write-Host "Читаю secrets/$Repo…"
$general = Parse-Env (Read-RemoteEnv '.env')
$project = Parse-Env (Read-RemoteEnv 'collegelms.env')

$merged = [ordered]@{}
foreach ($key in $general.Keys) { $merged[$key] = $general[$key] }
foreach ($key in $project.Keys) { $merged[$key] = $project[$key] }

$existingLines = @()
if (Test-Path $Target) {
    $existingLines = Get-Content -LiteralPath $Target
}

$seen = @{}
$output = [System.Collections.Generic.List[string]]::new()
foreach ($line in $existingLines) {
    if ($line -match '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$' -and $merged.Contains($matches[1])) {
        $key = $matches[1]
        $output.Add("$key=$($merged[$key])")
        $seen[$key] = $true
    }
    else {
        $output.Add($line)
    }
}

$added = 0
foreach ($key in $merged.Keys) {
    if (-not $seen.ContainsKey($key)) {
        $output.Add("$key=$($merged[$key])")
        $added++
    }
}

# Гарантируем перевод строки в конце и записываем атомарно.
$content = ($output -join "`n").TrimEnd("`n") + "`n"
$temp = "$Target.tmp-$([guid]::NewGuid().ToString('N'))"
[System.IO.File]::WriteAllText($temp, $content, [System.Text.UTF8Encoding]::new($false))
Move-Item -LiteralPath $temp -Destination $Target -Force

Write-Host "Готово: ключей из secrets $($merged.Count), добавлено $added, файл $Target"
Write-Host ("Ключи: " + (($merged.Keys | Sort-Object) -join ', '))

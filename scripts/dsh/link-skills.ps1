<#
.SYNOPSIS
    Подключает скиллы OpenCode и Superpowers к DeepSeek Harness через junction'ы.

.DESCRIPTION
    DSH сканирует каталог $DSH_HOME\skills (по умолчанию ~/.dsh/skills).
    Скрипт создаёт в нём junction на каждый каталог скилла из:
      * ~/.config/opencode/skills          — глобальные скиллы OpenCode;
      * <superpowers>/skills               — скиллы плагина obra/superpowers.

    Сами скиллы не копируются: обновление источника сразу видно в DSH.
    Существующие каталоги и junction'ы не перезаписываются.

.EXAMPLE
    pwsh -File scripts/dsh/link-skills.ps1
#>
[CmdletBinding()]
param(
    [string]$DshHome = $(if ($env:DSH_HOME) { $env:DSH_HOME } else { Join-Path $env:USERPROFILE '.dsh' })
)

$ErrorActionPreference = 'Stop'

$skillRoot = Join-Path $DshHome 'skills'
New-Item -ItemType Directory -Force -Path $skillRoot | Out-Null
Write-Host "Каталог скиллов DSH: $skillRoot"

$sources = [System.Collections.Generic.List[string]]::new()

$openCodeSkills = Join-Path $env:USERPROFILE '.config\opencode\skills'
if (Test-Path $openCodeSkills) {
    $sources.Add($openCodeSkills)
}

$npmCache = Join-Path $env:USERPROFILE '.cache\opencode\npm'
if (Test-Path $npmCache) {
    $superpowersRoot = Get-ChildItem $npmCache -Directory -Filter 'git-superpowers-*' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($superpowersRoot) {
        $superpowersSkills = Get-ChildItem $superpowersRoot.FullName -Recurse -Directory -Filter 'skills' |
            Where-Object { $_.Parent -and $_.Parent.Name -eq 'superpowers' } |
            Select-Object -First 1
        if ($superpowersSkills) {
            $sources.Add($superpowersSkills.FullName)
        }
    }
}

if ($sources.Count -eq 0) {
    Write-Warning 'Не найдено ни одного источника скиллов. Проверьте ~/.config/opencode/skills.'
    return
}

$created = 0
$skipped = 0

foreach ($source in $sources) {
    Write-Host "Источник: $source"
    foreach ($skillDir in Get-ChildItem $source -Directory) {
        if ($skillDir.Name.StartsWith('.')) { continue }
        if (-not (Test-Path (Join-Path $skillDir.FullName 'SKILL.md'))) { continue }

        $linkPath = Join-Path $skillRoot $skillDir.Name
        if (Test-Path $linkPath) {
            $skipped++
            continue
        }

        New-Item -ItemType Junction -Path $linkPath -Target $skillDir.FullName | Out-Null
        $created++
    }
}

Write-Host "Готово: создано $created, пропущено (уже есть) $skipped."
Write-Host 'Проверка: Get-ChildItem $env:DSH_HOME\skills | Select-Object Name,LinkType'

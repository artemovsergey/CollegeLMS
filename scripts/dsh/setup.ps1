<#
.SYNOPSIS
    Разовая настройка рабочей среды DeepSeek Harness для CollegeLMS.

.DESCRIPTION
    Идемпотентно:
      1. создаёт junction .dsh/skills -> .opencode/skills;
      2. подключает глобальные скиллы OpenCode и Superpowers в ~/.dsh/skills;
      3. собирает .env из приватного репозитория secrets;
      4. проверяет наличие gh, node, GitHub MCP server и Playwright MCP.

    Bundle dsh-collegelms устанавливается отдельно через plugin_manager:
    правка профиля в обход plugin manager не поддерживается.

.EXAMPLE
    pwsh -File scripts/dsh/setup.ps1
    pwsh -File scripts/dsh/setup.ps1 -SkipSecrets
#>
[CmdletBinding()]
param(
    [switch]$SkipSecrets
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir '..\..')).Path
Push-Location $repoRoot
try {
    Write-Host '== 1. Junction .dsh/skills =='
    New-Item -ItemType Directory -Force -Path (Join-Path $repoRoot '.dsh') | Out-Null
    $projectSkills = Join-Path $repoRoot '.dsh\skills'
    if (Test-Path $projectSkills) {
        Write-Host "Уже существует: $projectSkills"
    }
    else {
        New-Item -ItemType Junction -Path $projectSkills -Target (Join-Path $repoRoot '.opencode\skills') | Out-Null
        Write-Host "Создан: $projectSkills"
    }

    Write-Host '== 2. Глобальные скиллы =='
    & (Join-Path $scriptDir 'link-skills.ps1')

    if (-not $SkipSecrets) {
        Write-Host '== 3. .env из secrets =='
        & (Join-Path $scriptDir 'sync-secrets.ps1')
    }
    else {
        Write-Host '== 3. .env пропущен (-SkipSecrets) =='
    }

    Write-Host '== 4. Проверка зависимостей =='
    $checks = [ordered]@{
        'gh авторизован'          = { (& gh auth status 2>&1) -match 'Logged in' }
        'node'                    = { Test-Path 'C:\Program Files\nodejs\node.exe' }
        'github-mcp-server'       = { Test-Path (Join-Path $env:USERPROFILE '.config\opencode\bin\github-mcp-server.exe') }
        'playwright-mcp'          = { Test-Path (Join-Path $env:APPDATA 'npm\node_modules\@playwright\mcp\cli.js') }
    }
    foreach ($name in $checks.Keys) {
        $ok = & $checks[$name]
        Write-Host ("  [{0}] {1}" -f $(if ($ok) { 'ok' } else { '!!' }), $name)
    }

    Write-Host ''
    Write-Host 'Готово. Осталось:'
    Write-Host '  * проверить plugin_manager -> list_bundles: dsh-collegelms enabled;'
    Write-Host '  * открыть новую сессию (политика моделей фиксируется при создании);'
    Write-Host '  * скилл dsh-environment — проверка и ограничения.'
}
finally {
    Pop-Location
}

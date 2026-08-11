# Коммит всех изменений и пуш в текущую ветку.
# Использование: /push <сообщение коммита>
# Требует GITHUB_TOKEN или GH_TOKEN (см. AGENTS.md — правило перед любым push).

$Message = $args -join " "

if ([string]::IsNullOrWhiteSpace($Message)) {
    Write-Error "Укажите сообщение коммита: /push <сообщение>"
    exit 1
}

if (-not $env:GITHUB_TOKEN -and -not $env:GH_TOKEN) {
    Write-Error "GITHUB_TOKEN или GH_TOKEN не установлен — пуш прерван (см. AGENTS.md)."
    exit 1
}

git add -A
git commit -m $Message
git push

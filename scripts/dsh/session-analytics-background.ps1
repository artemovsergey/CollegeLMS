# Скрытый запуск анализатора сессий из планировщика.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $root 'run.cmd') --quiet
exit $LASTEXITCODE

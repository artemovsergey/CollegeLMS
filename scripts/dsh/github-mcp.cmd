@echo off
rem Запуск GitHub MCP server для DeepSeek Harness.
rem Токен не хранится в конфиге: он берётся из keyring GitHub CLI
rem и передаётся дочернему процессу только через переменную окружения.
setlocal DisableDelayedExpansion

set "GH_EXE=C:\Users\asv\AppData\Local\Programs\GitHub CLI\bin\gh.exe"
set "MCP_EXE=C:\Users\asv\.config\opencode\bin\github-mcp-server.exe"

for /f "usebackq delims=" %%T in (`"%GH_EXE%" auth token 2^>nul`) do set "GITHUB_PERSONAL_ACCESS_TOKEN=%%T"
if not defined GITHUB_PERSONAL_ACCESS_TOKEN (
  echo github-mcp: не удалось получить токен через gh auth token 1>&2
  exit /b 1
)

"%MCP_EXE%" stdio

@echo off
rem DSH Session Analytics launcher: prefers the Node runtime bundled with DSH
rem (it has zstd support), then falls back to node.exe from PATH.
setlocal
set "SCRIPT=%~dp0session-analytics.mjs"
set "NODE="

if exist "%USERPROFILE%\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\node\bin\node.exe" (
  set "NODE=%USERPROFILE%\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\node\bin\node.exe"
)

if not defined NODE for /d %%D in ("%USERPROFILE%\.dsh\dsh-runtimes\*") do (
  if not defined NODE if exist "%%D\dependencies\node\bin\node.exe" set "NODE=%%D\dependencies\node\bin\node.exe"
)

if not defined NODE where node >nul 2>nul && set "NODE=node"

if not defined NODE (
  echo [session-analytics] node.exe not found in PATH or in .dsh\dsh-runtimes 1>&2
  exit /b 1
)

"%NODE%" "%SCRIPT%" %*
exit /b %ERRORLEVEL%

@echo off
setlocal

cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass ^
  -File "%~dp0TextControlBox\Build\PackTextControlBox.ps1" ^
  -SourceName "TextControlBoxLocal"

set "exitCode=%ERRORLEVEL%"

echo.
if not "%exitCode%"=="0" (
    echo Pack failed with exit code %exitCode%.
) else (
    echo Pack completed successfully.
)

pause
exit /b %exitCode%

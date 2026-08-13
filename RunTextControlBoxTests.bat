@echo off
setlocal

cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass ^
  -File "%~dp0TextControlBox.Tests\Build\RunWinUITests.ps1"

set "exitCode=%ERRORLEVEL%"

echo.
if not "%exitCode%"=="0" (
    echo Tests failed with exit code %exitCode%.
) else (
    echo All packaged WinUI tests passed.
)

pause
exit /b %exitCode%

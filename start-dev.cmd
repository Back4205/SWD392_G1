@echo off
setlocal
rem Run the local script in its own PowerShell process; keep machine policy unchanged.
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-dev.ps1" %*
exit /b %ERRORLEVEL%

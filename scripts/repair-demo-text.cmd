@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0repair-demo-text.ps1" %*
exit /b %ERRORLEVEL%

@echo off
setlocal
rem Compatibility entrypoint: repair all known demo display text, including user names.
call "%~dp0repair-demo-text.cmd" %*
exit /b %ERRORLEVEL%

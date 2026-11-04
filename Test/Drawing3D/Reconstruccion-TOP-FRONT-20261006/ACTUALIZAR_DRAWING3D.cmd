@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Update-ReferenceAssemblyBundle.ps1"
set "UPDATE_RESULT=%ERRORLEVEL%"
pause
exit /b %UPDATE_RESULT%

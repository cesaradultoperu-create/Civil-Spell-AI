@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Actualizar.ps1"
set updateExit=%errorlevel%
pause
exit /b %updateExit%

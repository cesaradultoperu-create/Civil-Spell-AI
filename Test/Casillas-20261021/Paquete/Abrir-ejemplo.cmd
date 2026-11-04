@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Abrir-ejemplo.ps1"
if errorlevel 1 pause

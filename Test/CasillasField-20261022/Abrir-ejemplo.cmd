@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Field-v2\Abrir-Field.ps1"
if errorlevel 1 pause

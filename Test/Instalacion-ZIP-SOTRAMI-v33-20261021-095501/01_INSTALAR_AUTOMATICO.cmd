@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Instalar-SOTRAMI.ps1"
if errorlevel 1 echo NO SE COMPLETO LA INSTALACION. REVISE EL MENSAJE ANTERIOR.
pause

@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Cargar-SOTRAMI.ps1"
if errorlevel 1 echo NO SE COMPLETO LA CARGA. REVISE EL MENSAJE ANTERIOR.
pause

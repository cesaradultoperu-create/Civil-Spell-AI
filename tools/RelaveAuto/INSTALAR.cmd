@echo off
setlocal
echo Instalacion de RELAVEAUTO para Civil 3D 2023, 2024, 2025 y 2026
echo Guarde sus dibujos y cierre Civil 3D antes de instalar.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Instalar.ps1"
if errorlevel 1 (
 echo.
 echo No se completo la instalacion. Revise el mensaje anterior y GUIA_INSTALACION.txt.
) else (
 echo.
 echo Abra Civil 3D y escriba RELAVEAUTO. Las ventanas deben indicar 0.3.5.
)
echo.
pause

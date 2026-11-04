@echo off
title CivilSpellAI - Tramos DWG para Civil 3D 2023 y 2024
echo Cierre todas las ventanas de AutoCAD y Civil 3D antes de continuar.
echo Este archivo se debe ejecutar con clic derecho - Ejecutar como administrador.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Manage-TramosDWG.ps1" -Action Update -Scope AllUsers
if errorlevel 1 (
  echo.
  echo No se completo la instalacion. Conserve el mensaje de error que aparece arriba.
) else (
  echo.
  echo Instalacion completada. Abra Civil 3D y ejecute REVISARTRAMOSDWG.
)
pause

@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-SOTRAMI.ps1" -BuildDirectory "%~dp0payload"
if errorlevel 1 (
  echo.
  echo No se completo la instalacion. Revisa el mensaje anterior.
  pause
  exit /b 1
)
echo.
echo Instalacion completada. Abre Civil 3D 2024 para usar la ayuda Field.
pause

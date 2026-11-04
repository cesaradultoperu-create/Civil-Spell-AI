@echo off
setlocal
echo Instalador de DrenBordes 0.1.1 para Civil 3D 2024
echo Cierra Civil 3D antes de continuar con la instalacion.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Instalar-DrenBordes.ps1"
if errorlevel 1 (
  echo.
  echo No se completo la instalacion. Consulta el mensaje anterior y la guia.
  pause
  exit /b 1
)
echo.
echo Abre Civil 3D 2024 y escribe DRENBORDES.
echo Si la carga se bloquea, consulta ABRIR_GUIA.html en esta misma carpeta.
pause

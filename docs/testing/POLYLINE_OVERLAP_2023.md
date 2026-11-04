# Variante de polilíneas para Civil 3D 2023 (2026-10-06)

El usuario identificó Civil 3D 2023 en el equipo del destinatario después de
instalar el paquete completo 1.3.3.0. Ese paquete declara solamente series
R24.3, R25.0 y R25.1 (2024–2026). NETLOAD de la DLL 2024 pudo registrar los
comandos, pero esto no certifica soporte para 2023 ni atribuye a la versión
los quiebres que siguen visibles. La captura de Drawing1 muestra una cerrada,
elevación cero y longitud 30.553; no contiene el resumen de Limpiar ni el DWG.

CivilSpellAI.Overlap2023.csproj compila exclusivamente las cinco fuentes
compartidas del flujo de polilíneas contra AutoCAD.NET 24.2.0, publicado por
Autodesk para AutoCAD 2023, con destino .NET Framework 4.8/x64.
La variante usa AssemblyName CivilSpellAI.Overlap2023 y versión 1.3.3.1.
El manifiesto se limita a Civil3D R24.2 y el bundle y los respaldos de instalación
tienen nombres independientes del complemento completo. No se amplía la
compatibilidad del resto de las herramientas ni cambia la geometría corregible.

Fuentes primarias:
- https://www.nuget.org/packages/AutoCAD.NET/24.2.0
- https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-Customization/files/GUID-A6C680F2-DE2E-418A-A182-E4884073338A.htm

Validación realizada sin Civil 3D 2023 instalado:
- Debug y Release compilan sin errores ni advertencias.
- Debug pasa 15 verificaciones, incluida geometría y cierre colineal.
  artifacts/validation/overlap-correction-5cb2f551df034cc0b20746ba18c531d1/console.log
- Release pasa 12 verificaciones de ciclo, límites de selección, fragmentos,
  cancelación, U y restauración.
  artifacts/validation/overlap-correction-4bca63c5c6194a99bd14282e71097622/console.log
- Ambas se ejecutaron en AutoCAD Core Console 2024 con perfil aislado.
  El runner comprueba que el comando emita el identificador de la variante
  2023, para no aceptar como evidencia la ejecución de la DLL completa.
- ZIP verificado: cinco archivos, instalador con sintaxis válida, manifiesto
  R24.2, DLL idéntica por hash y ausencia de DLL redistribuidas de Autodesk.

Comandos de reproducción:
```powershell
dotnet restore CivilSpellAI.Overlap2023.csproj --packages .packages
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-OverlapCorrection.ps1 -Target2023 -Configuration Debug
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-OverlapCorrection.ps1 -Target2023 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-Overlap2023Bundle.ps1
```
HostYear 2023 permite ejecutar las regresiones en un equipo de desarrollo que
sí disponga de Core Console y plantilla de esa versión. Por defecto es 2024.

Entrega separada: artifacts/entrega-solapes-2023/CivilSpellAI-Polilineas-2023-1.3.3.1.zip.
No se reemplazó la entrega 2024 ni se instaló esta variante en el equipo local.
El destinatario debe extraer el ZIP, cerrar todas las instancias de AutoCAD y
ejecutar INSTALAR_2023.cmd como administrador. Esto reutiliza el instalador
administrado con copia verificada y respaldo. La carga debe mostrar:
`CIVILSPELLAI POLILINEAS 2023 - revision 1.3.3.1 (SDK 24.2)`.

Pendiente: probar instalación/autocarga y operaciones en Civil 3D 2023 real,
y recibir el DWG guardado o los resúmenes de Limpiar/Comprobar del caso restante.
No se afirma que el cambio de versión elimine las esquinas reales del dibujo.
